using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for CPS Golf (Club Prophet Systems) tee time systems.
/// Authenticates via direct OAuth2 password grant against the CPS identity server.
/// Uses a short-lived token for read operations (TeeTimes search) and the main
/// access token for write operations (TeeTimePricesCalculation, SaveTeeTime).
/// </summary>
public class CpsGolfAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<CpsGolfAdapter> _logger;
    private readonly IHttpClientFactory     _httpClientFactory;

    // OAuth token
    private string? _bearerToken;       // main access token (client_id=js1, 1-hr expiry)
    private string? _shortLivedToken;   // short-lived token (optional fallback)

    // Site config resolved at login
    private string? _baseUrl;           // e.g. "https://paramus.cps.golf"
    private int     _courseId    = 3;   // Paramus Golf Course default
    private int     _siteId      = 4;   // Paramus site default
    private string  _componentId = "1";
    private string  _websiteId   = "eac579f2-7b7d-4aa2-b1fc-08daa2e7b047"; // Paramus default
    private string  _classCode   = "RS";
    private string  _memberStoreId = "1";
    private int     _golferId    = 0;
    private string  _acct        = "0";
    private string? _lastLoginError;

    // CPS Golf OIDC client credentials (public — from /onlineresweb/assets/env.js)
    private const string OidcClientId     = "js1";
    private const string OidcClientSecret = "v4secret";

    // Full scope required for booking (includes sale/inventory for SaveTeeTime)
    private const string OidcScope =
        "openid profile onlinereservation sale inventory sh customer email recommend references";

    public CpsGolfAdapter(ILogger<CpsGolfAdapter> logger, IHttpClientFactory httpClientFactory)
    {
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string? LoginErrorMessage => _lastLoginError;

    // ──────────────────────────────────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<bool> LoginAsync(string url, string email, string password,
        CancellationToken ct = default)
    {
        _lastLoginError = null;
        try
        {
            var uri  = new Uri(url.StartsWith("http") ? url : "https://" + url);
            _baseUrl = $"{uri.Scheme}://{uri.Host}";
            _logger.LogInformation("CPS Golf: Authenticating {Email} against {BaseUrl}", email, _baseUrl);

            var client = _httpClientFactory.CreateClient("CpsGolf");

            // ── Step 1: Get main access token via ROPC grant ──────────────────
            var tokenEndpoint = $"{_baseUrl}/identityapi/connect/token";
            var tokenRequest  = new FormUrlEncodedContent(new Dictionary<string, string>
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

            _logger.LogInformation("CPS Golf: Token endpoint → {Status}", tokenResponse.StatusCode);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _lastLoginError = $"Authentication failed ({(int)tokenResponse.StatusCode}): {responseBody}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            using var tokenDoc = JsonDocument.Parse(responseBody);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenEl))
            {
                _lastLoginError = $"Token response missing access_token: {responseBody[..Math.Min(300, responseBody.Length)]}";
                return false;
            }

            _bearerToken = accessTokenEl.GetString();
            if (string.IsNullOrEmpty(_bearerToken))
            {
                _lastLoginError = "access_token was empty";
                return false;
            }

            _logger.LogInformation("CPS Golf: Main token obtained");

            // Extract user claims from JWT (golferId, acct, classCode, storeId)
            ExtractUserClaimsFromJwt();

            // ── Step 2: Exchange for short-lived token (used for TeeTimes search) ──
            await TryGetShortLivedTokenAsync(ct);

            // ── Step 3: Fetch course list to get courseId + siteId ────────────
            await FetchCourseInfoAsync(ct);

            _logger.LogInformation(
                "CPS Golf: Ready — courseId={CourseId} siteId={SiteId} golferId={GolferId} classCode={ClassCode}",
                _courseId, _siteId, _golferId, _classCode);
            return true;
        }
        catch (Exception ex)
        {
            _lastLoginError = ex.Message;
            _logger.LogError(ex, "CPS Golf: Login exception for {Email}", email);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Search
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<List<TeeTimeSlot>> SearchAvailableSlotsAsync(
        DateTime date, TimeSpan preferredTime, int windowMinutes, int players,
        CancellationToken ct = default)
    {
        var slots = new List<TeeTimeSlot>();
        try
        {
            // Register transaction (use short-lived token)
            var transactionId = await RegisterTransactionAsync(useShortToken: true, ct);

            // Format date: "Sat Mar 28 2026"
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
                             $"&classCode={_classCode}" +
                             $"&defaultOnlineRate=N" +
                             $"&isUseCapacityPricing=false" +
                             $"&memberStoreId={_memberStoreId}" +
                             $"&searchType=1";

            var (rawBody, teeTimes) = await CallApiRawAsync<JsonElement>(
                "GET", searchUrl,
                useShortToken: true,
                extraHeaders: new Dictionary<string, string> { ["x-correlation-id"] = transactionId },
                ct: ct);

            _logger.LogInformation("CPS Golf: TeeTimes raw response (first 800 chars): {Body}",
                rawBody.Length > 800 ? rawBody[..800] : rawBody);

            if (teeTimes.ValueKind == JsonValueKind.Undefined)
            {
                var errMsg = rawBody.Length > 300 ? rawBody[..300] : rawBody;
                _logger.LogWarning("CPS Golf: TeeTimes API error — {Raw}", errMsg);
                throw new InvalidOperationException($"TeeTimes search failed: {errMsg}");
            }

            var teeTimesArray = teeTimes.ValueKind == JsonValueKind.Array
                ? teeTimes
                : teeTimes.TryGetProperty("teeTimes", out var nested)  ? nested
                : teeTimes.TryGetProperty("TeeTimes", out var nestedU) ? nestedU
                : default;

            if (teeTimesArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("CPS Golf: TeeTimes response was not an array. Kind={Kind}", teeTimes.ValueKind);
                return slots;
            }

            int total = 0;
            foreach (var tt in teeTimesArray.EnumerateArray())
            {
                total++;
                try
                {
                    // CPS Golf uses "startTime" for the tee time datetime
                    string? timeStr = GetStringProp(tt,
                        "startTime", "StartTime",
                        "teeOffDateTime", "TeeOffDateTime",
                        "teeOffTime", "TeeOffTime");

                    if (!DateTime.TryParse(timeStr, out var slotDt)) continue;

                    if (!IsWithinWindow(slotDt.TimeOfDay, preferredTime, windowMinutes)) continue;

                    // CPS Golf uses "participants" for available slots
                    int available = GetIntProp(tt,
                        "participants", "Participants",
                        "availableSlots", "AvailableSlots",
                        "openSlots", "OpenSlots",
                        "maxGuest", "MaxGuest") ?? 4;

                    if (available < players) continue;

                    // CPS Golf uses "teeSheetId" as the booking identifier
                    var teeSheetId = GetIntProp(tt, "teeSheetId", "TeeSheetId");
                    string slotId  = teeSheetId.HasValue
                        ? teeSheetId.Value.ToString()
                        : GetStringProp(tt, "teeTimeId", "TeeTimeId", "id", "Id")
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

            _logger.LogInformation("CPS Golf: Parsed {Total} total slots, {Match} within window", total, slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf: SearchAvailableSlotsAsync failed");
            throw;
        }
        return slots;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Book
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<BookingAdapterResult> BookSlotAsync(TeeTimeSlot slot, int players,
        CancellationToken ct = default)
    {
        var result = new BookingAdapterResult();
        try
        {
            if (!int.TryParse(slot.SlotId, out var teeSheetId))
            {
                result.ErrorMessage = $"Invalid teeSheetId '{slot.SlotId}' — expected integer";
                return result;
            }

            // Step 1: Get a fresh transaction ID (use main token for booking operations)
            var transactionId = await RegisterTransactionAsync(useShortToken: false, ct);

            // Step 2: Build booking list — participant 1 is the member, rest are unassigned
            var bookingList = BuildBookingList(teeSheetId, players, transactionId);

            var pricesPayload = new
            {
                selectedTeeSheetId      = teeSheetId,
                bookingList,
                holes                   = 18,
                numberOfPlayer          = players,
                numberOfRider           = 0,
                cartType                = 0,
                coupon                  = (string?)null,
                depositType             = 0,
                depositAmount           = 0,
                selectedValuePackageCode = (string?)null,
                isUseCapacityPricing    = false,
                thirdPartyId            = (string?)null,
                ibxCardOnFile           = (string?)null,
                transactionId,
                isPrepayDeposit         = false
            };

            // Step 3: TeeTimePricesCalculation (validates price/availability)
            var (pricesRaw, pricesResp) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/TeeTimePricesCalculation",
                pricesPayload, useShortToken: false, null, ct);

            _logger.LogInformation("CPS Golf: TeeTimePricesCalculation → {Body}",
                pricesRaw.Length > 500 ? pricesRaw[..500] : pricesRaw);

            // Step 4: CheckRestrictReservation
            var restrictPayload = new
            {
                teeSheetId,
                courseId  = _courseId,
                siteId    = _siteId,
                classCode = _classCode
            };

            var (restrictRaw, _) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/CheckRestrictReservation",
                restrictPayload, useShortToken: false, null, ct);

            _logger.LogInformation("CPS Golf: CheckRestrictReservation → {Body}",
                restrictRaw.Length > 200 ? restrictRaw[..200] : restrictRaw);

            // Step 5: Register a second transaction ID (required before SaveTeeTime)
            var bookingTransactionId = await RegisterTransactionAsync(useShortToken: false, ct);

            // Rebuild booking list with new transactionId for SaveTeeTime
            var saveBookingList = BuildBookingList(teeSheetId, players, bookingTransactionId);

            var savePayload = new
            {
                selectedTeeSheetId      = teeSheetId,
                bookingList             = saveBookingList,
                holes                   = 18,
                numberOfPlayer          = players,
                numberOfRider           = 0,
                cartType                = 0,
                coupon                  = (string?)null,
                depositType             = 0,
                depositAmount           = 0,
                selectedValuePackageCode = (string?)null,
                isUseCapacityPricing    = false,
                thirdPartyId            = (string?)null,
                ibxCardOnFile           = (string?)null,
                transactionId           = bookingTransactionId,
                isPrepayDeposit         = false
            };

            // Step 6: SaveTeeTime
            var (rawBody, response) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/SaveTeeTime",
                savePayload, useShortToken: false, null, ct);

            _logger.LogInformation("CPS Golf: SaveTeeTime raw response: {Body}", rawBody);

            // Parse confirmation — CPS Golf typically returns teeReservationId or similar
            string? confirmationNumber = GetStringProp(response,
                "confirmationNumber", "ConfirmationNumber",
                "confirmationId",     "ConfirmationId",
                "reservationNumber",  "ReservationNumber",
                "reservationCode",    "ReservationCode",
                "receiptNumber",      "ReceiptNumber");

            if (string.IsNullOrEmpty(confirmationNumber))
            {
                var numericId = GetIntProp(response,
                    "teeReservationId", "TeeReservationId",
                    "reservationId",    "ReservationId",
                    "receiptId",        "ReceiptId",
                    "id",               "Id");
                if (numericId.HasValue && numericId.Value > 0)
                    confirmationNumber = numericId.Value.ToString();
            }

            if (!string.IsNullOrEmpty(confirmationNumber))
            {
                result.Success            = true;
                result.ConfirmationNumber = confirmationNumber;
                result.BookedTime         = slot.DateTime;
                _logger.LogInformation("CPS Golf: Booking confirmed — #{Confirmation}", confirmationNumber);
            }
            else
            {
                result.ErrorMessage = GetStringProp(response,
                    "message", "Message", "errorMessage", "ErrorMessage", "error", "Error")
                    ?? $"SaveTeeTime returned no confirmation ID. Response: {rawBody[..Math.Min(600, rawBody.Length)]}";
                _logger.LogWarning("CPS Golf: No confirmation ID — {Error}", result.ErrorMessage);
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

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls /identityapi/myconnect/token/short with the main Bearer token to obtain
    /// a short-lived token used for read operations (TeeTimes search, etc.).
    /// </summary>
    private async Task TryGetShortLivedTokenAsync(CancellationToken ct)
    {
        try
        {
            var client  = _httpClientFactory.CreateClient("CpsGolf");
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/identityapi/myconnect/token/short");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            // myconnect/token/short is a custom OAuth2-style endpoint — use form encoding
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = "onlinereswebshortlived"
            });

            var response = await client.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("CPS Golf: myconnect/token/short → {Status} {Body}",
                (int)response.StatusCode,
                body.Length > 200 ? body[..200] : body);

            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
            {
                // Response could be a bare JWT string or {"access_token":"..."}
                if (body.TrimStart().StartsWith("\"") || body.TrimStart().StartsWith("ey"))
                {
                    _shortLivedToken = body.Trim().Trim('"');
                }
                else
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("access_token", out var el))
                        _shortLivedToken = el.GetString();
                    else if (doc.RootElement.TryGetProperty("token", out var el2))
                        _shortLivedToken = el2.GetString();
                }

                if (!string.IsNullOrEmpty(_shortLivedToken))
                    _logger.LogInformation("CPS Golf: Short-lived token obtained");
                else
                    _logger.LogWarning("CPS Golf: Could not parse short-lived token from body");
            }
            else
            {
                _logger.LogWarning("CPS Golf: Short-lived token request failed — will use main token for search");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: TryGetShortLivedTokenAsync failed — will use main token");
        }
    }

    /// <summary>
    /// Fetches OnlineCourses and picks the 18-hole course, extracting courseId and siteId.
    /// </summary>
    private async Task FetchCourseInfoAsync(CancellationToken ct)
    {
        try
        {
            var courses = await CallApiAsync<JsonElement>(
                "GET",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/OnlineCourses",
                useShortToken: false, ct: ct);

            if (courses.ValueKind != JsonValueKind.Array) return;

            // Prefer the 18-hole course (isDefaultSelected or holes==18)
            JsonElement? chosen = null;
            foreach (var c in courses.EnumerateArray())
            {
                var holes = GetIntProp(c, "holes", "Holes") ?? 18;
                if (holes == 18) { chosen = c; break; }
            }
            chosen ??= courses.EnumerateArray().FirstOrDefault();

            if (chosen is not null)
            {
                _courseId  = GetIntProp(chosen.Value,    "courseId",  "CourseId")  ?? _courseId;
                _siteId    = GetIntProp(chosen.Value,    "siteId",    "SiteId")    ?? _siteId;
                _websiteId = GetStringProp(chosen.Value, "websiteId", "WebsiteId") ?? _websiteId;
                _logger.LogInformation("CPS Golf: courseId={CourseId} siteId={SiteId} websiteId={WebsiteId}",
                    _courseId, _siteId, _websiteId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: FetchCourseInfoAsync failed — using defaults");
        }
    }

    /// <summary>
    /// Decodes the JWT access token to extract user-specific booking fields.
    /// </summary>
    private void ExtractUserClaimsFromJwt()
    {
        if (string.IsNullOrEmpty(_bearerToken)) return;
        try
        {
            var parts = _bearerToken.Split('.');
            if (parts.Length < 2) return;

            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                             .Replace('-', '+').Replace('_', '/');

            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = doc.RootElement;

            _logger.LogInformation("CPS Golf: JWT claims = {Claims}", root.ToString());

            if (int.TryParse(GetStringProp(root, "golferId")  ?? "", out var gi)) _golferId = gi;
            _acct          = GetStringProp(root, "acct")          ?? _acct;
            _classCode     = GetStringProp(root, "classCode")     ?? _classCode;
            _memberStoreId = GetStringProp(root, "store_id")      ?? _memberStoreId;
            _componentId   = GetStringProp(root, "component_id")  ?? _componentId;

            _logger.LogInformation(
                "CPS Golf: User claims — golferId={GolferId} acct={Acct} classCode={ClassCode} storeId={StoreId}",
                _golferId, _acct, _classCode, _memberStoreId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: JWT decode failed");
        }
    }

    /// <summary>
    /// Calls RegisterTransactionId and returns the transaction GUID.
    /// CPS Golf returns the ID in the x-correlation-id response header.
    /// Falls back to JSON body parsing, then a random GUID.
    /// </summary>
    private async Task<string> RegisterTransactionAsync(bool useShortToken, CancellationToken ct)
    {
        var fallback = Guid.NewGuid().ToString();
        try
        {
            var client  = _httpClientFactory.CreateClient("CpsGolf");
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId");

            var token = useShortToken && !string.IsNullOrEmpty(_shortLivedToken)
                ? _shortLivedToken
                : _bearerToken;
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            request.Headers.TryAddWithoutValidation("client-id",         "onlineresweb");
            request.Headers.TryAddWithoutValidation("x-componentid",     _componentId);
            request.Headers.TryAddWithoutValidation("x-siteid",          _siteId.ToString());
            request.Headers.TryAddWithoutValidation("x-websiteid",       _websiteId);
            request.Headers.TryAddWithoutValidation("x-moduleid",        "7");
            request.Headers.TryAddWithoutValidation("x-productid",       "1");
            request.Headers.TryAddWithoutValidation("x-terminalid",      "3");
            request.Headers.TryAddWithoutValidation("x-ismobile",        "false");
            request.Headers.TryAddWithoutValidation("x-requestid",       Guid.NewGuid().ToString());
            request.Headers.TryAddWithoutValidation("x-timezone-offset", "240");
            request.Headers.TryAddWithoutValidation("x-timezoneid",      "America/New_York");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("CPS Golf: RegisterTransactionId → {Status}", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CPS Golf: RegisterTransactionId error {Status}: {Body}",
                    (int)response.StatusCode,
                    body.Length > 300 ? body[..300] : body);
                return fallback;
            }

            // Primary: read from x-correlation-id response header
            if (response.Headers.TryGetValues("x-correlation-id", out var vals))
            {
                var id = vals.FirstOrDefault();
                if (!string.IsNullOrEmpty(id))
                {
                    _logger.LogInformation("CPS Golf: TransactionId from x-correlation-id = {Id}", id);
                    return id;
                }
            }

            // Fallback: parse JSON body
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var id = GetStringProp(doc.RootElement, "transactionId", "TransactionId");
                if (!string.IsNullOrEmpty(id))
                {
                    _logger.LogInformation("CPS Golf: TransactionId from body = {Id}", id);
                    return id;
                }
            }

            _logger.LogWarning("CPS Golf: RegisterTransactionId — no ID found, using random GUID. Body: {Body}", body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: RegisterTransactionId failed — using random GUID");
        }
        return fallback;
    }

    /// <summary>
    /// Builds the bookingList array for TeeTimePricesCalculation and SaveTeeTime.
    /// Participant 1 is the authenticated member; additional players are unassigned.
    /// </summary>
    private object[] BuildBookingList(int teeSheetId, int players, string transactionId)
    {
        var list = new List<object>();
        for (int i = 1; i <= players; i++)
        {
            bool isFirst = i == 1;
            list.Add(new
            {
                teeSheetId,
                holes               = 18,
                participantNo       = i,
                golferId            = _golferId,
                dependentId         = "0",
                rateCode            = "RG",
                isUnAssignedPlayer  = !isFirst,
                memberClassCode     = _classCode,
                memberStoreId       = _memberStoreId,
                cartType            = 0,
                playerId            = "0",
                acct                = _acct,
                isGuestOf           = false,
                isUseCapacityPricing = false,
                isSmartCard         = false
            });
        }
        return list.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HTTP helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<(string RawBody, T Parsed)> CallApiRawAsync<T>(
        string method, string url,
        object? body = null, bool useShortToken = false,
        Dictionary<string, string>? extraHeaders = null,
        CancellationToken ct = default)
    {
        var client  = _httpClientFactory.CreateClient("CpsGolf");
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Use short-lived token for read operations (RegisterTransactionId, TeeTimes search);
        // fall back to main token when short-lived isn't available or for write operations.
        var token = useShortToken && !string.IsNullOrEmpty(_shortLivedToken)
            ? _shortLivedToken
            : _bearerToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Required CPS Golf custom headers (discovered from browser DevTools)
        request.Headers.TryAddWithoutValidation("client-id",          "onlineresweb");
        request.Headers.TryAddWithoutValidation("x-componentid",      _componentId);
        request.Headers.TryAddWithoutValidation("x-siteid",           _siteId.ToString());
        request.Headers.TryAddWithoutValidation("x-websiteid",        _websiteId);
        request.Headers.TryAddWithoutValidation("x-moduleid",         "7");
        request.Headers.TryAddWithoutValidation("x-productid",        "1");
        request.Headers.TryAddWithoutValidation("x-terminalid",       "3");
        request.Headers.TryAddWithoutValidation("x-ismobile",         "false");
        request.Headers.TryAddWithoutValidation("x-requestid",        Guid.NewGuid().ToString());
        request.Headers.TryAddWithoutValidation("x-timezone-offset",  "240");
        request.Headers.TryAddWithoutValidation("x-timezoneid",       "America/New_York");

        // Caller-supplied extra headers (e.g. x-correlation-id for TeeTimes)
        if (extraHeaders != null)
            foreach (var kv in extraHeaders)
                request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        // Always send application/json — server returns 415 if Content-Type is absent on POST
        var bodyJson = body != null ? JsonSerializer.Serialize(body) : "{}";
        if (method == "POST" || body != null)
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, ct);
        var content  = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("CPS Golf API {Method} {Url} → {Status}",
            method, url, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CPS Golf API error {Status}: {Body}",
                (int)response.StatusCode,
                content.Length > 500 ? content[..500] : content);
            return (content, default!);
        }

        if (string.IsNullOrWhiteSpace(content)) return (content, default!);
        try { return (content, JsonSerializer.Deserialize<T>(content)!); }
        catch { return (content, default!); }
    }

    private async Task<T> CallApiAsync<T>(string method, string url,
        object? body = null, bool useShortToken = false,
        Dictionary<string, string>? extraHeaders = null,
        CancellationToken ct = default)
    {
        var (_, parsed) = await CallApiRawAsync<T>(method, url, body, useShortToken, extraHeaders, ct);
        return parsed;
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

    private static bool IsWithinWindow(TimeSpan slotTime, TimeSpan preferred, int windowMinutes)
        => (slotTime - preferred).Duration() <= TimeSpan.FromMinutes(windowMinutes);

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
