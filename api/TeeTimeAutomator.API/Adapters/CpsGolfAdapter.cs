using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for CPS Golf (Club Prophet Systems) tee time systems.
/// Authenticates via direct OAuth2 password grant against the CPS identity server —
/// no browser automation required. All subsequent calls use HttpClient.
/// </summary>
public class CpsGolfAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<CpsGolfAdapter> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private string? _baseUrl;       // scheme + host only, e.g. "https://paramus.cps.golf"
    private string? _bearerToken;
    private string? _componentId;
    private int     _courseId;
    private string? _lastLoginError;

    // CPS Golf OIDC client credentials — sourced from /onlineresweb/assets/env.js (public)
    private const string OidcClientId     = "js1";
    private const string OidcClientSecret = "v4secret";
    private const string OidcScope        = "openid profile onlinereservation";

    public CpsGolfAdapter(ILogger<CpsGolfAdapter> logger, IHttpClientFactory httpClientFactory)
    {
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Login — direct OAuth2 password grant, no Playwright
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>The actual error message from the last failed login attempt.</summary>
    public string? LoginErrorMessage => _lastLoginError;

    public async Task<bool> LoginAsync(string url, string email, string password,
        CancellationToken ct = default)
    {
        _lastLoginError = null;
        try
        {
            var uri = new Uri(url.StartsWith("http") ? url : "https://" + url);
            _baseUrl = $"{uri.Scheme}://{uri.Host}";
            _logger.LogInformation("CPS Golf: Authenticating {Email} against {BaseUrl}", email, _baseUrl);

            var client = _httpClientFactory.CreateClient("CpsGolf");

            // POST to OIDC token endpoint using Resource Owner Password Credentials grant
            var tokenEndpoint = $"{_baseUrl}/identityapi/connect/token";
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "password",
                ["username"]      = email,
                ["password"]      = password,
                ["client_id"]     = OidcClientId,
                ["client_secret"] = OidcClientSecret,
                ["scope"]         = OidcScope
            });

            var tokenResponse = await client.PostAsync(tokenEndpoint, tokenRequest, ct);
            var responseBody  = await tokenResponse.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("CPS Golf: Token endpoint returned {Status}", tokenResponse.StatusCode);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _lastLoginError = $"Authentication failed ({(int)tokenResponse.StatusCode}): {responseBody}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            using var tokenDoc = JsonDocument.Parse(responseBody);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenEl))
            {
                _lastLoginError = $"Token response missing access_token. Body: {responseBody[..Math.Min(300, responseBody.Length)]}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            _bearerToken = accessTokenEl.GetString();
            if (string.IsNullOrEmpty(_bearerToken))
            {
                _lastLoginError = "access_token was empty in token response";
                return false;
            }

            _logger.LogInformation("CPS Golf: OAuth token obtained successfully");

            // Step 1: Try to extract componentId from the JWT token claims
            TryExtractComponentIdFromJwt();

            // Step 2: If not in JWT, try GetAllOptions (it returns componentId for some sites)
            if (string.IsNullOrEmpty(_componentId))
            {
                var (rawOptions, options) = await CallApiRawAsync<JsonElement>(
                    "GET",
                    $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/GetAllOptions/{GetSiteCode()}",
                    ct: ct);

                _logger.LogInformation("CPS Golf: GetAllOptions response: {Body}",
                    rawOptions.Length > 1000 ? rawOptions[..1000] : rawOptions);

                if (options.ValueKind == JsonValueKind.Object)
                {
                    // Try every plausible field name
                    foreach (var name in new[] { "componentId", "ComponentId", "compId", "CompId", "comp_id" })
                    {
                        if (options.TryGetProperty(name, out var cid))
                        {
                            _componentId = cid.ValueKind == JsonValueKind.String ? cid.GetString()
                                         : cid.ValueKind == JsonValueKind.Number ? cid.GetInt64().ToString()
                                         : null;
                            if (!string.IsNullOrEmpty(_componentId)) break;
                        }
                    }
                }
            }

            // Fetch courseId from online courses list
            var courses = await CallApiAsync<JsonElement>(
                "GET",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/OnlineCourses",
                ct: ct);

            if (courses.ValueKind == JsonValueKind.Array && courses.GetArrayLength() > 0)
            {
                var first = courses.EnumerateArray().First();
                _courseId = first.TryGetProperty("courseId", out var cId)  ? cId.GetInt32()  :
                            first.TryGetProperty("CourseId", out var cIdU) ? cIdU.GetInt32() : 3;
            }
            else
            {
                _courseId = 3; // Paramus default fallback
            }

            _logger.LogInformation("CPS Golf: Ready — courseId={CourseId} componentId={ComponentId}",
                _courseId, _componentId);
            return true;
        }
        catch (Exception ex)
        {
            _lastLoginError = ex.Message;
            _logger.LogError(ex, "CPS Golf: Login exception for {Email}", email);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Search
    // ──────────────────────────────────────────────────────────────────────

    public async Task<List<TeeTimeSlot>> SearchAvailableSlotsAsync(
        DateTime date, TimeSpan preferredTime, int windowMinutes, int players,
        CancellationToken ct = default)
    {
        var slots = new List<TeeTimeSlot>();
        try
        {
            // Register a transaction ID (required by CPS Golf API)
            var txResponse = await CallApiAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId",
                ct: ct);

            string transactionId = Guid.NewGuid().ToString();
            if (txResponse.ValueKind == JsonValueKind.Object)
            {
                if (txResponse.TryGetProperty("transactionId", out var tx) && tx.ValueKind == JsonValueKind.String)
                    transactionId = tx.GetString() ?? transactionId;
                else if (txResponse.TryGetProperty("TransactionId", out var txU) && txU.ValueKind == JsonValueKind.String)
                    transactionId = txU.GetString() ?? transactionId;
            }

            // Format date the way CPS Golf expects: "Sat Mar 28 2026"
            var searchDate = Uri.EscapeDataString(date.ToString("ddd MMM dd yyyy"));
            var searchUrl  = $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/TeeTimes" +
                             $"?searchDate={searchDate}" +
                             $"&holes=18" +
                             $"&numberOfPlayer={players}" +
                             $"&courseIds={_courseId}" +
                             $"&searchTimeType=0" +
                             $"&transactionId={transactionId}" +
                             $"&teeOffTimeMin=0&teeOffTimeMax=23" +
                             $"&isChangeTeeOffTime=true" +
                             $"&teeSheetSearchView=5" +
                             $"&classCode=RS" +
                             $"&defaultOnlineRate=N" +
                             $"&isUseCapacityPricing=false" +
                             $"&memberStoreId=1" +
                             $"&searchType=1";

            var teeTimes = await CallApiAsync<JsonElement>("GET", searchUrl, ct: ct);
            _logger.LogInformation("CPS Golf: TeeTimes response kind={Kind}", teeTimes.ValueKind);

            var teeTimesArray = teeTimes.ValueKind == JsonValueKind.Array
                ? teeTimes
                : teeTimes.TryGetProperty("teeTimes", out var nested)  ? nested
                : teeTimes.TryGetProperty("TeeTimes", out var nestedU) ? nestedU
                : default;

            if (teeTimesArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("CPS Golf: Unexpected TeeTimes response format. Full response: {Body}",
                    teeTimes.ToString()[..Math.Min(500, teeTimes.ToString().Length)]);
                return slots;
            }

            foreach (var tt in teeTimesArray.EnumerateArray())
            {
                try
                {
                    string? timeStr = GetStringProp(tt,
                        "teeOffDateTime", "TeeOffDateTime", "teeOffTime", "TeeOffTime",
                        "startTime", "StartTime");

                    if (!DateTime.TryParse(timeStr, out var slotDt)) continue;

                    if (!IsWithinWindow(slotDt.TimeOfDay, preferredTime, windowMinutes)) continue;

                    int available = GetIntProp(tt,
                        "availableSlots", "AvailableSlots", "openSlots", "OpenSlots",
                        "availablePlayers", "maxGuest", "MaxGuest") ?? 4;

                    if (available < players) continue;

                    string slotId = GetStringProp(tt,
                        "teeTimeId", "TeeTimeId", "scheduleId", "ScheduleId", "id", "Id")
                        ?? slotDt.ToString("HHmm");

                    slots.Add(new TeeTimeSlot
                    {
                        SlotId           = slotId,
                        DateTime         = slotDt,
                        AvailablePlayers = available,
                        CourseName       = "Paramus Golf Course"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "CPS Golf: Error parsing tee time slot");
                }
            }

            _logger.LogInformation("CPS Golf: Found {Count} matching slots", slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf: SearchAvailableSlotsAsync failed");
        }
        return slots;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Book
    // ──────────────────────────────────────────────────────────────────────

    public async Task<BookingAdapterResult> BookSlotAsync(TeeTimeSlot slot, int players,
        CancellationToken ct = default)
    {
        var result = new BookingAdapterResult();
        try
        {
            var txResponse = await CallApiAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId",
                ct: ct);

            string transactionId = Guid.NewGuid().ToString();
            if (txResponse.ValueKind == JsonValueKind.Object &&
                txResponse.TryGetProperty("transactionId", out var tx) && tx.ValueKind == JsonValueKind.String)
                transactionId = tx.GetString() ?? transactionId;

            var bookingPayload = new
            {
                teeTimeId      = slot.SlotId,
                courseId       = _courseId,
                numberOfPlayer = players,
                transactionId,
                holes          = 18,
                classCode      = "RS"
            };

            var bookUrl = $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/SaveTeeTime";

            // Call with raw response logging so we can see exactly what CPS Golf returns
            var (rawBody, response) = await CallApiRawAsync<JsonElement>("POST", bookUrl, bookingPayload, ct);
            _logger.LogInformation("CPS Golf: SaveTeeTime raw response: {Body}", rawBody);

            // Try every plausible confirmation-number field name CPS Golf might return
            string? confirmationNumber = GetStringProp(response,
                "confirmationNumber", "ConfirmationNumber",
                "confirmationId",     "ConfirmationId",
                "reservationNumber",  "ReservationNumber",
                "teeReservationId",   "TeeReservationId",
                "reservationId",      "ReservationId",
                "reservationCode",    "ReservationCode",
                "receiptNumber",      "ReceiptNumber");

            // Also accept a numeric reservation ID as confirmation
            if (string.IsNullOrEmpty(confirmationNumber))
            {
                var numericId = GetIntProp(response,
                    "teeReservationId", "TeeReservationId",
                    "reservationId",    "ReservationId",
                    "receiptId",        "ReceiptId");
                if (numericId.HasValue)
                    confirmationNumber = numericId.Value.ToString();
            }

            // Only mark as booked when we have a real confirmation identifier.
            // A bare {"success":true} without an ID means the booking did NOT go through.
            if (!string.IsNullOrEmpty(confirmationNumber))
            {
                result.Success            = true;
                result.ConfirmationNumber = confirmationNumber;
                result.BookedTime         = slot.DateTime;
                _logger.LogInformation("CPS Golf: Booking confirmed — {Confirmation}", result.ConfirmationNumber);
            }
            else
            {
                result.ErrorMessage = GetStringProp(response, "message", "Message", "errorMessage", "ErrorMessage")
                    ?? $"Booking did not return a confirmation ID. Full response: {rawBody[..Math.Min(500, rawBody.Length)]}";
                _logger.LogWarning("CPS Golf: No confirmation ID in response — {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "CPS Golf: BookSlotAsync failed");
        }
        return result;
    }

    public Task LogoutAsync(CancellationToken ct = default) => Task.CompletedTask;

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Like CallApiAsync but also returns the raw response body string for logging/debugging.
    /// </summary>
    private async Task<(string RawBody, T Parsed)> CallApiRawAsync<T>(
        string method, string url, object? body = null, CancellationToken ct = default)
    {
        var client  = _httpClientFactory.CreateClient("CpsGolf");
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (!string.IsNullOrEmpty(_bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, ct);
        var content  = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("CPS Golf API {Method} {Url} → {Status}", method, url, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CPS Golf API error {Status}: {Body}", (int)response.StatusCode,
                content.Length > 500 ? content[..500] : content);
            return (content, default!);
        }

        if (string.IsNullOrWhiteSpace(content)) return (content, default!);

        try { return (content, JsonSerializer.Deserialize<T>(content)!); }
        catch { return (content, default!); }
    }

    private async Task<T> CallApiAsync<T>(string method, string url, object? body = null,
        CancellationToken ct = default)
    {
        var client  = _httpClientFactory.CreateClient("CpsGolf");
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (!string.IsNullOrEmpty(_bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, ct);
        var content  = await response.Content.ReadAsStringAsync(ct);

        _logger.LogDebug("CPS Golf API {Method} {Url} → {Status}", method, url, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CPS Golf API error {Status}: {Body}", (int)response.StatusCode,
                content.Length > 500 ? content[..500] : content);
            return default!;
        }

        if (string.IsNullOrWhiteSpace(content)) return default!;

        try { return JsonSerializer.Deserialize<T>(content)!; }
        catch { return default!; }
    }

    /// <summary>
    /// Decodes the JWT access token payload and logs all claims.
    /// Also tries to extract componentId from known claim names.
    /// </summary>
    private void TryExtractComponentIdFromJwt()
    {
        if (string.IsNullOrEmpty(_bearerToken)) return;
        try
        {
            var parts = _bearerToken.Split('.');
            if (parts.Length < 2) return;

            // JWT payload is base64url — pad and convert to standard base64
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                             .Replace('-', '+').Replace('_', '/');

            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));

            _logger.LogInformation("CPS Golf: JWT claims = {Claims}", doc.RootElement.ToString());

            // Try every plausible componentId claim name
            foreach (var name in new[] {
                "componentId", "ComponentId", "compId", "CompId",
                "comp_id", "component_id", "cid", "compid", "comp"
            })
            {
                if (doc.RootElement.TryGetProperty(name, out var el))
                {
                    _componentId = el.ValueKind == JsonValueKind.String ? el.GetString()
                                 : el.ValueKind == JsonValueKind.Number ? el.GetInt64().ToString()
                                 : null;
                    if (!string.IsNullOrEmpty(_componentId))
                    {
                        _logger.LogInformation("CPS Golf: componentId from JWT claim '{Name}': {Value}", name, _componentId);
                        return;
                    }
                }
            }

            _logger.LogWarning("CPS Golf: componentId not found in JWT — will try GetAllOptions");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: JWT decode failed");
        }
    }

    private string GetSiteCode()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return "paramus";
        var host = new Uri(_baseUrl).Host; // paramus.cps.golf
        return host.Split('.')[0];         // paramus
    }

    private static string? GetStringProp(JsonElement el, params string[] names)
    {
        foreach (var name in names)
            if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        return null;
    }

    private static int? GetIntProp(JsonElement el, params string[] names)
    {
        foreach (var name in names)
            if (el.TryGetProperty(name, out var p) &&
                p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
                return i;
        return null;
    }

    private static bool GetBoolProp(JsonElement el, params string[] names)
    {
        foreach (var name in names)
            if (el.TryGetProperty(name, out var p) &&
               (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
                return p.GetBoolean();
        return false;
    }

    private static bool IsWithinWindow(TimeSpan slotTime, TimeSpan preferred, int windowMinutes)
    {
        var diff = (slotTime - preferred).Duration();
        return diff <= TimeSpan.FromMinutes(windowMinutes);
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
