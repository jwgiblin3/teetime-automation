using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for ForeUp (foreup.com) golf booking systems
/// </summary>
public class ForeUpAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<ForeUpAdapter> _logger;
    private IBrowser? _browser;
    private IPage? _page;
    private string? _currentBaseUrl;

    private const int PageLoadTimeoutMs = 30000;
    private const int WaitForNavigationTimeoutMs = 15000;

    public ForeUpAdapter(ILogger<ForeUpAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string url, string email, string password, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("ForeUp adapter: Starting login for {Email} on {Url}", email, url);
            _currentBaseUrl = url;

            // Initialize browser if not already done
            if (_browser == null)
            {
                var playwright = await Playwright.CreateAsync();
                _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--disable-blink-features=AutomationControlled" }
                });
            }

            // Create new page
            _page = await _browser.NewPageAsync();
            _page.SetDefaultTimeout(PageLoadTimeoutMs);

            // Navigate to ForeUp login
            var loginUrl = url.Contains("/login") ? url : $"{url.TrimEnd('/')}/login";
            _logger.LogInformation("Navigating to ForeUp login page: {LoginUrl}", loginUrl);
            await _page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Find and fill email field
            var emailInput = await _page.QuerySelectorAsync("input[type='email'], input[name='email']");
            if (emailInput == null)
            {
                _logger.LogWarning("Email input not found on ForeUp login page");
                return false;
            }

            _logger.LogInformation("Entering email on ForeUp");
            await _page.FillAsync("input[type='email'], input[name='email']", email);

            // Find and fill password field
            var passwordInput = await _page.QuerySelectorAsync("input[type='password']");
            if (passwordInput == null)
            {
                _logger.LogWarning("Password input not found on ForeUp login page");
                return false;
            }

            _logger.LogInformation("Entering password on ForeUp");
            await _page.FillAsync("input[type='password']", password);

            // Click login button
            var loginButton = await _page.QuerySelectorAsync("button[type='submit'], button:has-text('Login'), button:has-text('Sign In')");
            if (loginButton != null)
            {
                _logger.LogInformation("Submitting ForeUp login form");
                await loginButton.ClickAsync();

                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout after ForeUp login");
                }
            }

            // Check for login errors
            var errorElement = await _page.QuerySelectorAsync(".error, .alert-danger, [role='alert']");
            if (errorElement != null)
            {
                var errorText = await errorElement.TextContentAsync();
                _logger.LogWarning("ForeUp login error: {ErrorText}", errorText);
                return false;
            }

            _logger.LogInformation("ForeUp login successful for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForeUp login failed for {Email}", email);
            return false;
        }
    }

    public async Task<List<TeeTimeSlot>> SearchAvailableSlotsAsync(
        DateTime date,
        TimeSpan preferredTime,
        int windowMinutes,
        int players,
        CancellationToken ct = default)
    {
        var slots = new List<TeeTimeSlot>();

        try
        {
            if (_page == null)
            {
                _logger.LogWarning("Page not initialized for ForeUp search");
                return slots;
            }

            _logger.LogInformation("ForeUp: Searching for slots on {Date} at {Time} for {Players} players",
                date.Date, preferredTime, players);

            // Navigate to booking/search page
            var bookingUrl = _currentBaseUrl?.TrimEnd('/') ?? "https://foreup.com";
            _logger.LogInformation("Navigating to ForeUp booking page: {BookingUrl}", bookingUrl);
            await _page.GotoAsync(bookingUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Set date
            var dateInput = await _page.QuerySelectorAsync("input[type='date'], input[name='date']");
            if (dateInput != null)
            {
                var dateString = date.ToString("yyyy-MM-dd");
                _logger.LogInformation("Setting search date to {Date}", dateString);
                await _page.FillAsync("input[type='date'], input[name='date']", dateString);
            }

            // Set preferred time
            var timeInput = await _page.QuerySelectorAsync("input[type='time'], input[name='time']");
            if (timeInput != null)
            {
                var timeString = preferredTime.ToString(@"hh\:mm");
                _logger.LogInformation("Setting preferred time to {Time}", timeString);
                await _page.FillAsync("input[type='time'], input[name='time']", timeString);
            }

            // Set player count
            var playerSelect = await _page.QuerySelectorAsync("select[name='players'], select[name='playerCount']");
            if (playerSelect != null)
            {
                _logger.LogInformation("Setting player count to {Players}", players);
                await _page.SelectOptionAsync("select[name='players'], select[name='playerCount']", players.ToString());
            }

            // Submit search
            var searchButton = await _page.QuerySelectorAsync("button[type='submit'], button:has-text('Search')");
            if (searchButton != null)
            {
                _logger.LogInformation("Submitting ForeUp search");
                await searchButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Parse results
            var slotElements = await _page.QuerySelectorAllAsync(".teetime-slot, [data-teetime], .availability-slot");
            _logger.LogInformation("Found {SlotCount} tee time slots", slotElements.Count);

            foreach (var element in slotElements)
            {
                try
                {
                    var textContent = await element.TextContentAsync();
                    var slotId = await element.GetAttributeAsync("data-teetime-id");

                    if (string.IsNullOrEmpty(textContent))
                        continue;

                    if (string.IsNullOrEmpty(slotId))
                        slotId = await element.GetAttributeAsync("id") ?? Guid.NewGuid().ToString();

                    // Parse time from text
                    if (!TryParseTimeFromText(textContent, date, out var slotDateTime))
                        continue;

                    // Check time window
                    if (!IsWithinTimeWindow(slotDateTime.TimeOfDay, preferredTime, windowMinutes))
                        continue;

                    // Parse available players (default to players if not found)
                    var availableText = await element.GetAttributeAsync("data-available");
                    int availablePlayers = 4;
                    if (!string.IsNullOrEmpty(availableText) && int.TryParse(availableText, out int parsed))
                        availablePlayers = parsed;

                    if (availablePlayers < players)
                        continue;

                    var slot = new TeeTimeSlot
                    {
                        SlotId = slotId,
                        DateTime = slotDateTime,
                        AvailablePlayers = availablePlayers,
                        CourseName = ExtractCourseNameFromElement(textContent)
                    };

                    slots.Add(slot);
                    _logger.LogDebug("Added ForeUp slot: {SlotId} at {DateTime}", slotId, slotDateTime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing ForeUp slot element");
                }
            }

            _logger.LogInformation("ForeUp search complete: found {SlotCount} matching slots", slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForeUp search failed");
        }

        return slots;
    }

    public async Task<BookingAdapterResult> BookSlotAsync(TeeTimeSlot slot, int players, CancellationToken ct = default)
    {
        var result = new BookingAdapterResult();

        try
        {
            if (_page == null)
            {
                result.ErrorMessage = "Page not initialized";
                _logger.LogWarning("Page not initialized for ForeUp booking");
                return result;
            }

            _logger.LogInformation("ForeUp: Booking slot {SlotId} at {DateTime} for {Players} players",
                slot.SlotId, slot.DateTime, players);

            // Find and click the slot
            var slotElement = await _page.QuerySelectorAsync($"[data-teetime-id='{slot.SlotId}']");
            if (slotElement == null)
            {
                result.ErrorMessage = "Slot not found on page";
                _logger.LogWarning("ForeUp slot {SlotId} not found", slot.SlotId);
                return result;
            }

            await slotElement.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Handle player count in booking form
            var playerCountInput = await _page.QuerySelectorAsync("input[name='playerCount'], select[name='players']");
            if (playerCountInput != null)
            {
                _logger.LogInformation("Setting player count to {Players} in booking form", players);
                var tagName = await playerCountInput.GetAttributeAsync("tagName");
                if (tagName?.ToLower() == "select")
                {
                    await _page.SelectOptionAsync("select[name='players'], select[name='playerCount']", players.ToString());
                }
                else
                {
                    await _page.FillAsync("input[name='playerCount']", players.ToString());
                }
            }

            // Complete booking
            var bookButton = await _page.QuerySelectorAsync("button[type='submit'], button:has-text('Book'), button:has-text('Confirm')");
            if (bookButton != null)
            {
                _logger.LogInformation("Submitting ForeUp booking");
                await bookButton.ClickAsync();

                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout after ForeUp booking submission");
                }
            }

            // Look for confirmation message/page
            var confirmationElement = await _page.QuerySelectorAsync(".confirmation, [data-confirmation], .success");
            if (confirmationElement != null)
            {
                var confirmationText = await confirmationElement.TextContentAsync();
                result.ConfirmationNumber = ExtractConfirmationNumber(confirmationText);
                result.BookedTime = slot.DateTime;
                result.Success = true;
                _logger.LogInformation("ForeUp booking successful: {ConfirmationNumber}", result.ConfirmationNumber);
            }
            else
            {
                // Check for error message
                var errorElement = await _page.QuerySelectorAsync(".error, .alert, [role='alert']");
                if (errorElement != null)
                {
                    result.ErrorMessage = await errorElement.TextContentAsync();
                }
                else
                {
                    result.ErrorMessage = "Booking may have failed - no confirmation found";
                }
                _logger.LogWarning("ForeUp booking may have failed: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "ForeUp booking failed");
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("ForeUp: Logging out");

            if (_page != null)
            {
                var logoutButton = await _page.QuerySelectorAsync("button:has-text('Logout'), button:has-text('Sign Out')");
                if (logoutButton != null)
                {
                    await logoutButton.ClickAsync();
                    try
                    {
                        await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                    }
                    catch
                    {
                        // Navigation may not occur
                    }
                }

                await _page.CloseAsync();
                _page = null;
            }

            _logger.LogInformation("ForeUp logout complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForeUp logout error");
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        try
        {
            if (_page != null)
                await _page.CloseAsync();

            if (_browser != null)
                await _browser.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing ForeUp adapter");
        }
    }

    private static bool TryParseTimeFromText(string? text, DateTime date, out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrEmpty(text))
            return false;

        var timePattern = @"(\d{1,2}):(\d{2})\s*(AM|PM)?";
        var match = System.Text.RegularExpressions.Regex.Match(text, timePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        var hour = int.Parse(match.Groups[1].Value);
        var minute = int.Parse(match.Groups[2].Value);
        var meridiem = match.Groups[3].Value?.ToUpper();

        if (!string.IsNullOrEmpty(meridiem))
        {
            if (meridiem == "PM" && hour != 12)
                hour += 12;
            else if (meridiem == "AM" && hour == 12)
                hour = 0;
        }

        try
        {
            result = date.Date.AddHours(hour).AddMinutes(minute);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWithinTimeWindow(TimeSpan slotTime, TimeSpan preferredTime, int windowMinutes)
    {
        var windowStart = preferredTime.Add(TimeSpan.FromMinutes(-windowMinutes));
        var windowEnd = preferredTime.Add(TimeSpan.FromMinutes(windowMinutes));

        return slotTime >= windowStart && slotTime <= windowEnd;
    }

    private static string ExtractCourseNameFromElement(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "ForeUp Course";

        var lines = text.Split('\n');
        return lines.FirstOrDefault()?.Trim() ?? "ForeUp Course";
    }

    private static string ExtractConfirmationNumber(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var match = System.Text.RegularExpressions.Regex.Match(text, @"[A-Z0-9]{6,12}");
        return match.Success ? match.Value : string.Empty;
    }
}
