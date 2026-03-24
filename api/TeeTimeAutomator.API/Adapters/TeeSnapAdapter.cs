using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for TeeSnap booking systems
/// Note: Full automation requires course-specific TeeSnap URL configuration
/// </summary>
public class TeeSnapAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<TeeSnapAdapter> _logger;
    private IBrowser? _browser;
    private IPage? _page;
    private string? _currentBaseUrl;

    private const int PageLoadTimeoutMs = 30000;
    private const int WaitForNavigationTimeoutMs = 15000;

    public TeeSnapAdapter(ILogger<TeeSnapAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string url, string email, string password, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("TeeSnap adapter: Starting login for {Email} on {Url}", email, url);
            _currentBaseUrl = url;

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

            // Navigate to the TeeSnap login page
            _logger.LogInformation("Navigating to TeeSnap login at {Url}", url);
            await _page.GotoAsync(url, new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Look for email input
            var emailInput = await _page.QuerySelectorAsync("input[type='email'], input[name='email']");
            if (emailInput == null)
            {
                _logger.LogWarning("Email input not found on TeeSnap login page");
                return false;
            }

            _logger.LogInformation("Entering email on TeeSnap login");
            await _page.FillAsync("input[type='email'], input[name='email']", email);

            // Look for password input
            var passwordInput = await _page.QuerySelectorAsync("input[type='password']");
            if (passwordInput == null)
            {
                _logger.LogWarning("Password input not found on TeeSnap login page");
                return false;
            }

            _logger.LogInformation("Entering password on TeeSnap login");
            await _page.FillAsync("input[type='password']", password);

            // Submit login form
            var loginButton = await _page.QuerySelectorAsync("button[type='submit']");
            if (loginButton != null)
            {
                _logger.LogInformation("Submitting TeeSnap login form");
                await loginButton.ClickAsync();

                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout after TeeSnap login submission");
                }
            }

            // Check for login errors
            var errorElement = await _page.QuerySelectorAsync(".error, .alert-danger, [role='alert']");
            if (errorElement != null)
            {
                var errorText = await errorElement.TextContentAsync();
                _logger.LogWarning("TeeSnap login error: {ErrorText}", errorText);
                return false;
            }

            _logger.LogInformation("TeeSnap login successful for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeeSnap login failed for {Email}", email);
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
                _logger.LogWarning("Page not initialized for TeeSnap search");
                return slots;
            }

            _logger.LogInformation("TeeSnap: Searching for slots on {Date} at {Time} for {Players} players",
                date.Date, preferredTime, players);

            // Navigate to search/booking page
            var searchUrl = _currentBaseUrl?.TrimEnd('/') ?? "https://teesnap.com";
            _logger.LogInformation("Navigating to TeeSnap search page: {SearchUrl}", searchUrl);
            await _page.GotoAsync(searchUrl, new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Set date filter
            var dateInput = await _page.QuerySelectorAsync("input[type='date'], input[name='date']");
            if (dateInput != null)
            {
                var dateString = date.ToString("yyyy-MM-dd");
                _logger.LogInformation("Setting search date to {Date}", dateString);
                await _page.FillAsync("input[type='date'], input[name='date']", dateString);
            }

            // Set time filter
            var timeInput = await _page.QuerySelectorAsync("input[type='time'], input[name='time']");
            if (timeInput != null)
            {
                var timeString = preferredTime.ToString(@"hh\:mm");
                _logger.LogInformation("Setting search time to {Time}", timeString);
                await _page.FillAsync("input[type='time'], input[name='time']", timeString);
            }

            // Set player count if applicable
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
                _logger.LogInformation("Submitting TeeSnap search");
                await searchButton.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Parse available slots
            var slotElements = await _page.QuerySelectorAllAsync(".tee-time, [data-slot], .slot");
            _logger.LogInformation("Found {SlotCount} slot elements", slotElements.Count);

            foreach (var element in slotElements)
            {
                try
                {
                    var timeText = await element.TextContentAsync();
                    var slotId = await element.GetAttributeAsync("data-slot-id");

                    if (string.IsNullOrEmpty(timeText))
                        continue;

                    if (string.IsNullOrEmpty(slotId))
                        slotId = Guid.NewGuid().ToString();

                    // Parse time from text
                    if (!TryParseTimeFromText(timeText, date, out var slotDateTime))
                        continue;

                    // Check if within preferred window
                    if (!IsWithinTimeWindow(slotDateTime.TimeOfDay, preferredTime, windowMinutes))
                        continue;

                    var slot = new TeeTimeSlot
                    {
                        SlotId = slotId,
                        DateTime = slotDateTime,
                        AvailablePlayers = players,
                        CourseName = "TeeSnap Course"
                    };

                    slots.Add(slot);
                    _logger.LogDebug("Added TeeSnap slot: {SlotId} at {DateTime}", slotId, slotDateTime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing TeeSnap slot element");
                }
            }

            _logger.LogInformation("TeeSnap search complete: found {SlotCount} matching slots", slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeeSnap search failed");
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
                _logger.LogWarning("Page not initialized for TeeSnap booking");
                return result;
            }

            _logger.LogInformation("TeeSnap: Booking slot {SlotId} at {DateTime} for {Players} players",
                slot.SlotId, slot.DateTime, players);

            // Find and click the slot
            var slotElement = await _page.QuerySelectorAsync($"[data-slot-id='{slot.SlotId}']");
            if (slotElement == null)
            {
                result.ErrorMessage = "Slot not found on page";
                _logger.LogWarning("TeeSnap slot {SlotId} not found", slot.SlotId);
                return result;
            }

            await slotElement.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Handle player count selection in booking form
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
            var confirmButton = await _page.QuerySelectorAsync("button[type='submit'], button:has-text('Confirm'), button:has-text('Book')");
            if (confirmButton != null)
            {
                _logger.LogInformation("Confirming TeeSnap booking");
                await confirmButton.ClickAsync();

                try
                {
                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Navigation timeout after TeeSnap booking confirmation");
                }
            }

            // Look for confirmation
            var confirmationElement = await _page.QuerySelectorAsync(".confirmation, [data-confirmation], .success-message");
            if (confirmationElement != null)
            {
                var confirmationText = await confirmationElement.TextContentAsync();
                result.ConfirmationNumber = ExtractConfirmationNumber(confirmationText);
                result.BookedTime = slot.DateTime;
                result.Success = true;
                _logger.LogInformation("TeeSnap booking successful: {ConfirmationNumber}", result.ConfirmationNumber);
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
                _logger.LogWarning("TeeSnap booking may have failed: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "TeeSnap booking failed");
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("TeeSnap: Logging out");

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

            _logger.LogInformation("TeeSnap logout complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeeSnap logout error");
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
            _logger.LogError(ex, "Error disposing TeeSnap adapter");
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

    private static string ExtractConfirmationNumber(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var match = System.Text.RegularExpressions.Regex.Match(text, @"[A-Z0-9]{6,12}");
        return match.Success ? match.Value : string.Empty;
    }
}
