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
    private string? _dynamicClassCode;          // updated from TeeTimePrices defaultClassCode
    private string  _memberStoreId = "1";
    private int     _golferId    = 0;
    private string  _acct        = "0";
    private string? _email;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);
    private string? _lastLoginError;

    // CPS Golf OIDC client credentials (public — from /onlineresweb/assets/env.js)
    private const string OidcClientId     = "js1";
    private const string OidcClientSecret = "v4secret";

    // Full scope required for booking (includes sale/inventory for ReserveTeeTimes)
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
        _email = email;
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

            // Capture all cookies from login response (earliest opportunity)
            CaptureCookies(tokenResponse.Headers
                .TryGetValues("Set-Cookie", out var loginCookies) ? loginCookies : []);

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

            // Use the class code returned by TeeTimePrices if we have one — it may differ from the
            // raw JWT value (e.g. JWT gives "RS" but the pricing engine returns "1RBS").
            var effectiveClassCode = _dynamicClassCode ?? _classCode;

            // ISO date format (yyyy-MM-dd) — avoids encoding/space issues with "Sat Mar 28 2026" style
            var searchDate = date.ToString("yyyy-MM-dd");
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
                             $"&classCode={effectiveClassCode}" +
                             $"&defaultOnlineRate=N" +
                             $"&isUseCapacityPricing=false" +
                             $"&memberStoreId={_memberStoreId}" +
                             $"&searchType=1";

            _logger.LogInformation(
                "CPS Golf: TeeTimes search — classCode={ClassCode} (jwt={JwtClass} dynamic={DynamicClass})",
                effectiveClassCode, _classCode, _dynamicClassCode ?? "(none)");

            var (rawBody, teeTimes) = await CallApiRawAsync<JsonElement>(
                "GET", searchUrl,
                useShortToken: true,
                extraHeaders: new Dictionary<string, string>
                {
                    ["x-correlation-id"] = transactionId,
                    // Referer mirrors what the Angular SPA sends on every TeeTimes request
                    ["Referer"]          = $"{_baseUrl}/onlineresweb/teetime/book"
                },
                ct: ct);

            _logger.LogInformation("CPS Golf: TeeTimes raw response (first 800 chars): {Body}",
                rawBody.Length > 800 ? rawBody[..800] : rawBody);

            if (teeTimes.ValueKind == JsonValueKind.Undefined)
            {
                var errMsg = rawBody.Length > 300 ? rawBody[..300] : rawBody;
                _logger.LogWarning("CPS Golf: TeeTimes API error — {Raw}", errMsg);
                throw new InvalidOperationException($"TeeTimes search failed: {errMsg}");
            }

            if (teeTimes.TryGetProperty("isSuccess", out var isSuccessEl) &&
                isSuccessEl.ValueKind == JsonValueKind.False)
            {
                var errMsg = GetStringProp(teeTimes, "message", "Message", "errorMessage", "ErrorMessage")
                             ?? rawBody[..Math.Min(300, rawBody.Length)];
                _logger.LogWarning("CPS Golf: TeeTimes isSuccess=false — {Error}", errMsg);
                throw new InvalidOperationException($"TeeTimes search failed: {errMsg}");
            }

            var teeTimesArray = teeTimes.ValueKind == JsonValueKind.Array
                ? teeTimes
                : teeTimes.TryGetProperty("content",   out var content)  ? content
                : teeTimes.TryGetProperty("teeTimes",  out var nested)   ? nested
                : teeTimes.TryGetProperty("TeeTimes",  out var nestedU)  ? nestedU
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

            // ── Golden Thread: ONE transactionId registered upfront and threaded through every step ──
            // The CPS middleware validates a "chain of custody": RegisterTransactionId → Guid A →
            // CheckRestrictReservation (Guid A) → TeeTimePrices (Guid A) → ReserveTeeTimes (Guid A).
            // Previously we were generating separate IDs at each step, which broke the chain.

            // Step 1: Register the master transactionId (Guid A)
            var transactionId = await RegisterTransactionAsync(useShortToken: false, ct);
            _logger.LogInformation("CPS Golf: Master transactionId registered — {TxId}", transactionId);

            // Step 2: CheckRestrictReservation — send Guid A
            var (restrictRaw, _) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/CheckRestrictReservation",
                new { teeSheetId, courseId = _courseId, siteId = _siteId, classCode = _classCode, transactionId },
                useShortToken: false, null, ct);

            _logger.LogInformation("CPS Golf: CheckRestrictReservation → {Body}",
                restrictRaw.Length > 200 ? restrictRaw[..200] : restrictRaw);

            // Step 3: TeeTimePrices — pass Guid A so the server anchors it to this session.
            //         The server returns a bookingTransactionId (the cart/session token used by ReserveTeeTimes).
            var (pricesRaw, bookingTransactionId) = await GetTeeTimePricesTransactionIdAsync(
                teeSheetId, players, transactionId, ct);

            _logger.LogInformation("CPS Golf: TeeTimePrices → bookingTransactionId={Id} | Body={Body}",
                bookingTransactionId,
                pricesRaw.Length > 500 ? pricesRaw[..500] : pricesRaw);

            // Step 4: Register bookingTransactionId (Guid B) — the middleware requires
            // that BOTH Guid A (master) and Guid B (cart/pricing token) are registered
            // before ReserveTeeTimes will accept the call.
            _logger.LogInformation("CPS Golf: Registering bookingTransactionId (Guid B) — {Id}", bookingTransactionId);
            await RegisterTransactionAsync(useShortToken: false, ct, existingId: bookingTransactionId);

            // Step 5: Build booking list
            var bookingList = BuildBookingList(teeSheetId, players, bookingTransactionId);

            _logger.LogInformation(
                "CPS Golf: Ready to reserve — transactionId={TxId}  bookingTransactionId={BookingTxId}",
                transactionId, bookingTransactionId);

            // Step 6: ReserveTeeTimes
            // lockedTeeTimesSessionId is the cart/session token and must equal bookingTransactionId (Guid B).
            var checkoutReferer = $"{_baseUrl}/onlineresweb/teetime/checkout?id={teeSheetId}&holes=18&numberOfPlayer={players}";

            var reservePayload = new
            {
                cancelReservationLink = $"{_baseUrl}/onlineresweb/auth/verify-email?returnUrl=cancel-booking",
                homePageLink          = $"{_baseUrl}/onlineresweb/",
                affiliateId           = (string?)null,
                finalizeSaleModel     = new
                {
                    acct     = _acct,
                    playerId = 0,
                    isGuest  = false,
                    creditCardInfo = new
                    {
                        cardNumber  = (string?)null,
                        cardHolder  = (string?)null,
                        expireMM    = (string?)null,
                        expireYY    = (string?)null,
                        cvv         = (string?)null,
                        email       = _email,
                        cardToken   = (string?)null
                    },
                    monerisCC = (string?)null,
                    ibxCC     = (string?)null
                },
                sessionGuid             = (string?)null,
                lockedTeeTimesSessionId = bookingTransactionId,   // must match Guid B
                bookingTransactionId,
                transactionId
            };

            var (rawBody, response) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/ReserveTeeTimes",
                reservePayload, useShortToken: false,
                extraHeaders: new Dictionary<string, string> { ["Referer"] = checkoutReferer },
                ct);

            _logger.LogInformation("CPS Golf: ReserveTeeTimes raw response: {Body}", rawBody);

            if (response.ValueKind == JsonValueKind.Undefined)
            {
                result.ErrorMessage = rawBody.Trim('"');
                return result;
            }

            // Parse confirmation
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
    /// Calls TeeTimePrices, passing the already-registered <paramref name="existingTransactionId"/>
    /// (Guid A) so the server anchors pricing to that session.
    /// Returns the raw response body and the server-issued bookingTransactionId (the cart token
    /// that ReserveTeeTimes expects).  Also captures <c>defaultClassCode</c> for future searches.
    /// </summary>
    private async Task<(string RawBody, string BookingTransactionId)> GetTeeTimePricesTransactionIdAsync(
        int teeSheetId, int players, string existingTransactionId, CancellationToken ct)
    {
        // Only the booking member is in the list for the prices call
        var bookingList = BuildBookingList(teeSheetId, players, transactionId: existingTransactionId);

        var payload = new
        {
            selectedTeeSheetId       = teeSheetId,
            bookingList,
            holes                    = 18,
            numberOfPlayer           = players,
            numberOfRider            = 0,
            cartType                 = 0,
            coupon                   = (string?)null,
            depositType              = 0,
            depositAmount            = 0,
            selectedValuePackageCode = (string?)null,
            isUseCapacityPricing     = false,
            thirdPartyId             = (string?)null,
            ibxCardOnFile            = (string?)null,
            advancedBookingFee       = (string?)null,
            transactionId            = existingTransactionId,   // ← thread Guid A through
            isPrepayDeposit          = false
        };

        var referer = $"{_baseUrl}/onlineresweb/teetime/checkout?id={teeSheetId}&holes=18&numberOfPlayer={players}";
        var (rawBody, response) = await CallApiRawAsync<JsonElement>(
            "POST",
            $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/TeeTimePrices",
            payload, useShortToken: false,
            extraHeaders: new Dictionary<string, string> { ["Referer"] = referer },
            ct);

        // Capture defaultClassCode — may differ from the JWT classCode (e.g. "1RBS" vs "RS").
        // Store it so subsequent /TeeTimes searches use the correct restricted class.
        if (response.ValueKind != JsonValueKind.Undefined)
        {
            var dynClass = GetStringProp(response,
                "defaultClassCode", "DefaultClassCode", "classCode", "ClassCode");
            if (!string.IsNullOrEmpty(dynClass) && dynClass != _dynamicClassCode)
            {
                _logger.LogInformation(
                    "CPS Golf: TeeTimePrices returned defaultClassCode={DynClass} (was {Old})",
                    dynClass, _dynamicClassCode ?? _classCode);
                _dynamicClassCode = dynClass;
            }
        }

        // The server returns bookingTransactionId — this is the cart token for ReserveTeeTimes.
        string? bookingTransactionId = null;
        if (response.ValueKind != JsonValueKind.Undefined)
            bookingTransactionId = GetStringProp(response,
                "bookingTransactionId", "BookingTransactionId",
                "transactionId",       "TransactionId");

        if (string.IsNullOrEmpty(bookingTransactionId))
        {
            // Fall back: use the master transactionId as the booking token.
            // This keeps the chain intact even if the server doesn't echo it back.
            _logger.LogWarning(
                "CPS Golf: TeeTimePrices did not return a bookingTransactionId — using master transactionId as fallback");
            bookingTransactionId = existingTransactionId;
        }

        return (rawBody, bookingTransactionId);
    }

    /// <summary>
    /// Calls RegisterTransactionId and returns the transaction GUID.
    /// CPS Golf returns the ID in the x-correlation-id response header.
    /// Falls back to JSON body parsing, then a random GUID.
    /// </summary>
    private async Task<string> RegisterTransactionAsync(bool useShortToken, CancellationToken ct,
        string? existingId = null)
    {
        // If an existing ID is supplied (e.g. the one returned by TeeTimePrices) we register that;
        // otherwise we generate a fresh GUID for the server to register.
        var transactionId = existingId ?? Guid.NewGuid().ToString();
        try
        {
            var (body, _) = await CallApiRawAsync<JsonElement>(
                "POST",
                $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId",
                new { transactionId },
                useShortToken, null, ct);

            _logger.LogInformation("CPS Golf: RegisterTransactionId → Body: {Body}",
                body.Length > 200 ? body[..200] : body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CPS Golf: RegisterTransactionId exception — will proceed with generated GUID");
        }

        _logger.LogInformation("CPS Golf: Using transactionId = {Id}", transactionId);
        return transactionId;
    }

    /// <summary>
    /// Builds the bookingList array for TeeTimePricesCalculation and SaveTeeTime.
    /// Participant 1 is the authenticated member; additional players are unassigned.
    /// </summary>
    private object[] BuildBookingList(int teeSheetId, int players, string? transactionId)
    {
        var list = new List<object>();
        for (int i = 1; i <= players; i++)
        {
            bool isFirst = i == 1;
            list.Add(new
            {
                teeSheetId,
                transactionId,          // CPS requires this on every player object
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

        ApplyRequestHeaders(request, useShortToken, extraHeaders);

        // Always send application/json — server returns 415 if Content-Type is absent on POST
        var bodyJson = body != null ? JsonSerializer.Serialize(body) : "{}";
        if (method == "POST" || body != null)
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, ct);
        var content  = await response.Content.ReadAsStringAsync(ct);

        // Capture all cookies from response
        CaptureCookies(response.Headers
            .TryGetValues("Set-Cookie", out var setCookies) ? setCookies : []);

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

    /// <summary>
    /// Applies all standard CPS Golf request headers to an outgoing request.
    /// </summary>
    private void ApplyRequestHeaders(HttpRequestMessage request, bool useShortToken,
        Dictionary<string, string>? extraHeaders = null)
    {
        // Auth token
        var token = useShortToken && !string.IsNullOrEmpty(_shortLivedToken)
            ? _shortLivedToken : _bearerToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // CPS Golf custom headers
        foreach (var (k, v) in new Dictionary<string, string>
        {
            // Standard browser headers — CPS middleware filters on these to block bots
            ["accept"]             = "application/json, text/plain, */*",
            ["x-requested-with"]   = "XMLHttpRequest",
            // CPS-specific headers
            ["client-id"]          = "onlineresweb",
            ["x-componentid"]      = _componentId,
            ["x-siteid"]           = _siteId.ToString(),
            ["x-websiteid"]        = _websiteId,
            ["x-moduleid"]         = "7",
            ["x-productid"]        = "1",
            ["x-terminalid"]       = "3",
            ["x-ismobile"]         = "false",
            ["x-requestid"]        = Guid.NewGuid().ToString(),
            ["x-timezone-offset"]  = "240",
            ["x-timezoneid"]       = "America/New_York",
            // Browser cache-busting headers (sent by Angular on every request)
            ["origin"]             = _baseUrl ?? "",
            ["cache-control"]      = "no-cache, no-store, must-revalidate",
            ["pragma"]             = "no-cache",
            ["expires"]            = "Sat, 01 Jan 2000 00:00:00 GMT",
            ["if-modified-since"]  = "0",
        })
            request.Headers.TryAddWithoutValidation(k, v);

        // Caller-supplied overrides (e.g. Referer, x-correlation-id)
        if (extraHeaders != null)
            foreach (var (k, v) in extraHeaders)
                request.Headers.TryAddWithoutValidation(k, v);

        // Replay all captured cookies
        if (_cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("cookie",
                string.Join("; ", _cookies.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    /// <summary>
    /// Parses Set-Cookie headers and upserts each cookie into the shared jar.
    /// Only the name=value pair is kept (path/domain/expires attributes are discarded).
    /// </summary>
    private void CaptureCookies(IEnumerable<string> setCookieHeaders)
    {
        foreach (var header in setCookieHeaders)
        {
            var nameValue = header.Split(';')[0].Trim();
            var eq = nameValue.IndexOf('=');
            if (eq <= 0) continue;

            var name  = nameValue[..eq].Trim();
            var value = nameValue[(eq + 1)..].Trim();
            _cookies[name] = value;
            _logger.LogInformation("CPS Golf: Cookie set {Name}={Value}", name, value);
        }
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
