using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for CPS Golf (Club Prophet Systems) tee time systems.
/// Uses the site's own REST API rather than DOM scraping.
/// Login is handled via Playwright to obtain the OIDC bearer token,
/// then all subsequent calls use HttpClient for reliability and speed.
/// </summary>
public class CpsGolfAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<CpsGolfAdapter> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private IBrowser?  _browser;
    private IPage?     _page;
    private string?    _baseUrl;      // scheme + host only, e.g. "https://paramus.cps.golf"
    private string?    _bearerToken;
    private string?    _componentId;
    private int        _courseId;
    private string?    _lastLoginError; // surfaced to caller via LoginErrorMessage

    private const int TimeoutMs = 30000;

    public CpsGolfAdapter(ILogger<CpsGolfAdapter> logger, IHttpClientFactory httpClientFactory)
    {
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>The actual error message from the last failed login attempt.</summary>
    public string? LoginErrorMessage => _lastLoginError;

    public async Task<bool> LoginAsync(string url, string email, string password,
        CancellationToken ct = default)
    {
        _lastLoginError = null;
        try
        {
            _logger.LogInformation("CPS Golf: Starting login for {Email} on {Url}", email, url);

            // Always extract just scheme+host so API calls use the root domain
            var uri = new Uri(url.StartsWith("http") ? url : "https://" + url);
            _baseUrl = $"{uri.Scheme}://{uri.Host}";
            _logger.LogInformation("CPS Golf: Base URL resolved to {BaseUrl}", _baseUrl);

            // --- 1. Use Playwright to log in and capture the OIDC token ----
            var playwright = await Playwright.CreateAsync();
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args     = new[] { "--disable-blink-features=AutomationControlled" }
            });

            _page = await _browser.NewPageAsync();
            _page.SetDefaultTimeout(TimeoutMs);

            // CPS Golf login is always at /onlineresweb/auth/verify-email
            var loginUrl = $"{_baseUrl}/onlineresweb/auth/verify-email";
            _logger.LogInformation("CPS Golf: Navigating to login page {LoginUrl}", loginUrl);
            await _page.GotoAsync(loginUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Enter email
            // NOTE: There are two button[type='submit'] on this page — the header "Sign In"
            // link comes first in the DOM. We must use button.mat-accent to target the form button.
            var emailInput = await _page.QuerySelectorAsync("input[type='email']");
            if (emailInput == null)
            {
                _lastLoginError = $"Email field not found on login page ({loginUrl}). Page title: {await _page.TitleAsync()}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }
            // Use Type (not Fill) so Angular's change detection fires on each keystroke
            await _page.TypeAsync("input[type='email']", email, new PageTypeOptions { Delay = 50 });

            // Press Enter to submit — more reliable than clicking a button that starts disabled
            await _page.PressAsync("input[type='email']", "Enter");

            // Wait for navigation to /auth/login
            try
            {
                await _page.WaitForURLAsync("**/auth/login**",
                    new PageWaitForURLOptions { Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                var pageUrl = _page.Url;
                _lastLoginError = $"Did not navigate to /auth/login after entering email. Still at: {pageUrl}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            _logger.LogInformation("CPS Golf: Navigated to login page: {Url}", _page.Url);

            // Wait for password field on the /auth/login page
            try
            {
                await _page.WaitForSelectorAsync("input[type='password']",
                    new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch (TimeoutException)
            {
                _lastLoginError = $"Password field did not appear on /auth/login. URL: {_page.Url}";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            await _page.TypeAsync("input[type='password']", password, new PageTypeOptions { Delay = 50 });

            // Press Enter to submit the login form
            await _page.PressAsync("input[type='password']", "Enter");

            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 20000 });
            }
            catch (TimeoutException) { /* navigation may not fire */ }

            var currentUrl = _page.Url;
            _logger.LogInformation("CPS Golf: After login submit, current URL is {Url}", currentUrl);

            // --- 2. Extract bearer token from localStorage ---------
            // CPS Golf stores the token directly (not as an oidc.user: JSON object)
            // at the key: "online-reservation-v5-access_token"
            _bearerToken = await _page.EvaluateAsync<string?>(
                "() => localStorage.getItem('online-reservation-v5-access_token')");

            if (string.IsNullOrEmpty(_bearerToken))
            {
                // Fallback: dump all keys so we can diagnose
                var allKeys = await _page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
                var keys = string.Join(", ", allKeys ?? Array.Empty<string>());
                _lastLoginError = $"Token not found in localStorage after login. URL: {currentUrl}. Keys present: [{keys}]";
                _logger.LogWarning("CPS Golf: {Error}", _lastLoginError);
                return false;
            }

            // --- 3. Fetch site options to get componentId and courseId ------
            var options = await CallApiAsync<JsonElement>(
                "GET", $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/GetAllOptions/{GetSiteCode()}",
                ct: ct);

            // componentId comes from the options payload
            if (options.ValueKind == JsonValueKind.Object)
            {
                if (options.TryGetProperty("componentId", out var cid))
                    _componentId = cid.GetString();
                else if (options.TryGetProperty("ComponentId", out var cidU))
                    _componentId = cidU.GetString();
            }

            // courseId - fetch courses list and take first active
            var courses = await CallApiAsync<JsonElement>(
                "GET", $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/OnlineCourses", ct: ct);

            if (courses.ValueKind == JsonValueKind.Array && courses.GetArrayLength() > 0)
            {
                var first = courses.EnumerateArray().First();
                _courseId = first.TryGetProperty("courseId", out var cId) ? cId.GetInt32() :
                            first.TryGetProperty("CourseId", out var cIdU) ? cIdU.GetInt32() : 3;
            }
            else
            {
                _courseId = 3; // Paramus default
            }

            _logger.LogInformation(
                "CPS Golf: Login successful — courseId={CourseId} componentId={ComponentId}",
                _courseId, _componentId);
            return true;
        }
        catch (Exception ex)
        {
            _lastLoginError = ex.Message;
            _logger.LogError(ex, "CPS Golf: Login failed for {Email}", email);
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
                "POST", $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId",
                ct: ct);

            string transactionId = txResponse.TryGetProperty("transactionId", out var tx)
                ? tx.GetString() ?? Guid.NewGuid().ToString()
                : txResponse.TryGetProperty("TransactionId", out var txU)
                    ? txU.GetString() ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

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

            _logger.LogInformation("CPS Golf: Raw TeeTimes response kind={Kind}", teeTimes.ValueKind);

            var teeTimesArray = teeTimes.ValueKind == JsonValueKind.Array
                ? teeTimes
                : teeTimes.TryGetProperty("teeTimes", out var nested) ? nested
                : teeTimes.TryGetProperty("TeeTimes", out var nestedU) ? nestedU
                : default;

            if (teeTimesArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("CPS Golf: Unexpected TeeTimes response format");
                return slots;
            }

            foreach (var tt in teeTimesArray.EnumerateArray())
            {
                try
                {
                    // Extract the slot time
                    string? timeStr = GetStringProp(tt,
                        "teeOffDateTime", "TeeOffDateTime", "teeOffTime", "TeeOffTime",
                        "startTime", "StartTime");

                    if (!DateTime.TryParse(timeStr, out var slotDt))
                        continue;

                    // Check within window
                    if (!IsWithinWindow(slotDt.TimeOfDay, preferredTime, windowMinutes))
                        continue;

                    // Available player spots
                    int available = GetIntProp(tt,
                        "availableSlots", "AvailableSlots", "openSlots", "OpenSlots",
                        "availablePlayers", "maxGuest", "MaxGuest") ?? 4;

                    if (available < players)
                        continue;

                    // Slot identifier for booking
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
            _logger.LogError(ex, "CPS Golf: Search failed");
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
            // Register a fresh transaction ID for the booking
            var txResponse = await CallApiAsync<JsonElement>(
                "POST", $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/RegisterTransactionId",
                ct: ct);

            string transactionId = txResponse.TryGetProperty("transactionId", out var tx)
                ? tx.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            var bookingPayload = new
            {
                teeTimeId     = slot.SlotId,
                courseId      = _courseId,
                numberOfPlayer = players,
                transactionId,
                holes         = 18,
                classCode     = "RS"
            };

            var bookUrl = $"{_baseUrl}/onlineres/onlineapi/api/v1/onlinereservation/SaveTeeTime";
            var response = await CallApiAsync<JsonElement>("POST", bookUrl, bookingPayload, ct);

            // Check confirmation
            string? confirmationNumber = GetStringProp(response,
                "confirmationNumber", "ConfirmationNumber",
                "confirmationId",   "ConfirmationId",
                "reservationNumber","ReservationNumber");

            bool success = !string.IsNullOrEmpty(confirmationNumber)
                        || GetBoolProp(response, "success", "Success", "isSuccess", "IsSuccess");

            if (success)
            {
                result.Success            = true;
                result.ConfirmationNumber = confirmationNumber ?? $"CPS-{slot.SlotId}";
                result.BookedTime         = slot.DateTime;
                _logger.LogInformation("CPS Golf: Booking confirmed — {Confirmation}",
                    result.ConfirmationNumber);
            }
            else
            {
                result.ErrorMessage = GetStringProp(response,
                    "message", "Message", "errorMessage", "ErrorMessage")
                    ?? "Booking failed — no confirmation returned";
                _logger.LogWarning("CPS Golf: Booking failed — {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "CPS Golf: BookSlotAsync failed");
        }
        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (_page != null)
        {
            await _page.CloseAsync();
            _page = null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private async Task<T> CallApiAsync<T>(string method, string url, object? body = null,
        CancellationToken ct = default)
    {
        var client  = _httpClientFactory.CreateClient("CpsGolf");
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (!string.IsNullOrEmpty(_bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        if (!string.IsNullOrEmpty(_componentId))
            request.Headers.TryAddWithoutValidation("componentid", _componentId);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, ct);
        var content  = await response.Content.ReadAsStringAsync(ct);

        _logger.LogDebug("CPS Golf API {Method} {Url} → {Status}", method, url,
            (int)response.StatusCode);

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

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        try
        {
            if (_page    != null) await _page.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf: Dispose error");
        }
    }
}
