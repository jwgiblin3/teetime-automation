using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for GolfNow (golfnow.com)
/// </summary>
public class GolfNowAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<GolfNowAdapter> _logger;
    private IBrowser? _browser;
    private IPage? _page;

    private const int PageLoadTimeoutMs = 30000;
    private const int WaitForNavigationTimeoutMs = 15000;

    public GolfNowAdapter(ILogger<GolfNowAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string url, string email, string password, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GolfNow adapter: Starting login for {Email}", email);

            // Initialize browser if not already done
            if (_browser == null)
            {
                var playwright = await Playwright.CreateAsync();
                _browser = await playwright.Chromium.LaunchAsync(new BrowserLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--disable-blink-features=AutomationControlled" }
                });
            }

            // Create new page
            _page = await _browser.NewPageAsync();
            _page.SetDefaultTimeout(PageLoadTimeoutMs);

            // Navigate to GolfNow login
            _logger.LogInformation("Navigating to GolfNow sign-in page");
            await _page.GotoAsync("https://www.golfnow.com/signin", new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Enter email
            var emailInput = await _page.QuerySelectorAsync("input[type='email'], input[name='email']");
            if (emailInput == null)
            {
                _logger.LogWarning("Email input not found on GolfNow login page");
                return false;
            }

            _logger.LogInformation("Entering email on GolfNow");
            await _page.FillAsync("input[type='email'], input[name='email']", email);

            // Click continue/next button
            var continueButton = await _page.QuerySelectorAsync("button:has-text('Continue'), button:has-text('Next')");
            if (continueButton != null)
            {
                await continueButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Enter password
            var passwordInput = await _page.QuerySelectorAsync("input[type='password']");
            if (passwordInput == null)
            {
                _logger.LogWarning("Password input not found on GolfNow");
                return false;
            }

            _logger.LogInformation("Entering password on GolfNow");
            await _page.FillAsync("input[type='password']", password);

            // Click sign in button
            var signInButton = await _page.QuerySelectorAsync("button:has-text('Sign In'), button[type='submit']");
            if (signInButton != null)
            {
                await signInButton.ClickAsync();
                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout after GolfNow sign in");
                }
            }

            // Check for login errors
            var errorElement = await _page.QuerySelectorAsync(".error, .alert-error, [role='alert']");
            if (errorElement != null)
            {
                var errorText = await errorElement.TextContentAsync();
                _logger.LogWarning("GolfNow login error: {ErrorText}", errorText);
                return false;
            }

            _logger.LogInformation("GolfNow login successful for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GolfNow login failed for {Email}", email);
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
                _logger.LogWarning("Page not initialized for GolfNow search");
                return slots;
            }

            _logger.LogInformation("GolfNow: Searching for slots on {Date} at {Time} for {Players} players",
                date.Date, preferredTime, players);

            // Navigate to tee times search
            await _page.GotoAsync("https://www.golfnow.com/search/teetime", new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Set date
            var dateInput = await _page.QuerySelectorAsync("input[type='date'], input[name='date']");
            if (dateInput != null)
            {
                var dateString = date.ToString("yyyy-MM-dd");
                _logger.LogInformation("Setting date to {Date}", dateString);
                await _page.FillAsync("input[type='date'], input[name='date']", dateString);
            }

            // Set time
            var timeInput = await _page.QuerySelectorAsync("input[type='time'], input[name='time']");
            if (timeInput != null)
            {
                var timeString = preferredTime.ToString(@"hh\:mm");
                _logger.LogInformation("Setting time to {Time}", timeString);
                await _page.FillAsync("input[type='time'], input[name='time']", timeString);
            }

            // Set player count
            var playerCountSelect = await _page.QuerySelectorAsync("select[name='players'], select[name='playerCount']");
            if (playerCountSelect != null)
            {
                _logger.LogInformation("Setting player count to {Players}", players);
                await _page.SelectOptionAsync("select[name='players'], select[name='playerCount']", players.ToString());
            }

            // Submit search
            var searchButton = await _page.QuerySelectorAsync("button:has-text('Search'), button[type='submit']");
            if (searchButton != null)
            {
                _logger.LogInformation("Submitting GolfNow search");
                await searchButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Parse results
            var slotElements = await _page.QuerySelectorAllAsync(".tee-time-result, [data-teetime], .teetime-slot");
            _logger.LogInformation("Found {SlotCount} tee time results", slotElements.Count);

            foreach (var element in slotElements)
            {
                try
                {
                    var timeText = await element.TextContentAsync();
                    var slotId = await element.GetAttributeAsync("data-teetime-id");

                    if (string.IsNullOrEmpty(timeText))
                        continue;

                    if (string.IsNullOrEmpty(slotId))
                        slotId = await element.GetAttributeAsync("id") ?? Guid.NewGuid().ToString();

                    // Extract time
                    if (!TryParseTimeFromText(timeText, date, out var slotDateTime))
                        continue;

                    // Check time window
                    if (!IsWithinTimeWindow(slotDateTime.TimeOfDay, preferredTime, windowMinutes))
                        continue;

                    var slot = new TeeTimeSlot
                    {
                        SlotId = slotId,
                        DateTime = slotDateTime,
                        AvailablePlayers = players,
                        CourseName = ExtractCourseNameFromElement(timeText)
                    };

                    slots.Add(slot);
                    _logger.LogDebug("Added GolfNow slot: {SlotId} at {DateTime}", slotId, slotDateTime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing GolfNow slot element");
                }
            }

            _logger.LogInformation("GolfNow search complete: found {SlotCount} matching slots", slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GolfNow search failed");
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
                _logger.LogWarning("Page not initialized for GolfNow booking");
                return result;
            }

            _logger.LogInformation("GolfNow: Booking slot {SlotId} at {DateTime} for {Players} players",
                slot.SlotId, slot.DateTime, players);

            // Click the tee time slot
            var slotElement = await _page.QuerySelectorAsync($"[data-teetime-id='{slot.SlotId}'], #{slot.SlotId}");
            if (slotElement == null)
            {
                result.ErrorMessage = "Slot not found";
                _logger.LogWarning("GolfNow slot {SlotId} not found", slot.SlotId);
                return result;
            }

            await slotElement.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Handle any player count selection if needed
            var playerCountInput = await _page.QuerySelectorAsync("input[name='playerCount'], select[name='players']");
            if (playerCountInput != null)
            {
                _logger.LogInformation("Setting player count to {Players} during booking", players);
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

            // Click book button
            var bookButton = await _page.QuerySelectorAsync("button:has-text('Book'), button:has-text('Reserve'), button[type='submit']");
            if (bookButton != null)
            {
                _logger.LogInformation("Clicking book button on GolfNow");
                await bookButton.ClickAsync();
                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout during GolfNow booking");
                }
            }

            // Look for confirmation
            var confirmationElement = await _page.QuerySelectorAsync(".confirmation, .booking-confirmation, [data-confirmation-number]");
            if (confirmationElement != null)
            {
                var confirmationText = await confirmationElement.TextContentAsync();
                result.ConfirmationNumber = ExtractConfirmationNumber(confirmationText);
                result.BookedTime = slot.DateTime;
                result.Success = true;
                _logger.LogInformation("GolfNow booking successful: {ConfirmationNumber}", result.ConfirmationNumber);
            }
            else
            {
                var errorElement = await _page.QuerySelectorAsync(".error, .alert, [role='alert']");
                if (errorElement != null)
                {
                    result.ErrorMessage = await errorElement.TextContentAsync();
                }
                else
                {
                    result.ErrorMessage = "Booking may have failed - no confirmation found";
                }
                _logger.LogWarning("GolfNow booking may have failed: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "GolfNow booking failed");
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("GolfNow: Logging out");

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

            _logger.LogInformation("GolfNow logout complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GolfNow logout error");
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
            _logger.LogError(ex, "Error disposing GolfNow adapter");
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
            return "GolfNow Course";

        // Try to extract course name (first line or before the time)
        var lines = text.Split('\n');
        return lines.FirstOrDefault()?.Trim() ?? "GolfNow Course";
    }

    private static string ExtractConfirmationNumber(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var match = System.Text.RegularExpressions.Regex.Match(text, @"[A-Z0-9]{6,12}");
        return match.Success ? match.Value : string.Empty;
    }
}
