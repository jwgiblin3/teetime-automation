using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Booking adapter for CPS Golf tee time systems (paramus.cps.golf style)
/// </summary>
public class CpsGolfAdapter : IBookingAdapter, IAsyncDisposable
{
    private readonly ILogger<CpsGolfAdapter> _logger;
    private IBrowser? _browser;
    private IPage? _page;
    private string? _currentBaseUrl;

    private const int PageLoadTimeoutMs = 30000;
    private const int WaitForNavigationTimeoutMs = 15000;

    public CpsGolfAdapter(ILogger<CpsGolfAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string url, string email, string password, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("CPS Golf adapter: Starting login for {Email} on {Url}", email, url);
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

            // Step 1: Navigate to verify-email page
            var loginUrl = $"{url.TrimEnd('/')}/auth/verify-email";
            _logger.LogInformation("Navigating to {LoginUrl}", loginUrl);
            await _page.GotoAsync(loginUrl, new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Step 2: Enter email
            var emailInput = await _page.QuerySelectorAsync("input[type='email']");
            if (emailInput == null)
            {
                _logger.LogWarning("Email input field not found on CPS Golf login page");
                return false;
            }

            _logger.LogInformation("Entering email on CPS Golf login form");
            await _page.FillAsync("input[type='email']", email);
            await _page.ClickAsync("button[type='submit']");

            // Wait for password field to appear
            try
            {
                await _page.WaitForSelectorAsync("input[type='password']", new PageWaitForSelectorOptions { Timeout = WaitForNavigationTimeoutMs });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Password field did not appear after email submission on CPS Golf");
                return false;
            }

            // Step 3: Enter password
            _logger.LogInformation("Entering password on CPS Golf login form");
            await _page.FillAsync("input[type='password']", password);
            await _page.ClickAsync("button[type='submit']");

            // Wait for navigation to complete
            try
            {
                await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Navigation timeout after password submission on CPS Golf");
            }

            // Verify we're logged in by checking if an error message appears
            var errorSelector = "div[role='alert'], .alert-danger, .error";
            var errorElement = await _page.QuerySelectorAsync(errorSelector);
            if (errorElement != null)
            {
                var errorText = await errorElement.TextContentAsync();
                _logger.LogWarning("Login error on CPS Golf: {ErrorText}", errorText);
                return false;
            }

            _logger.LogInformation("CPS Golf login successful for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf login failed for {Email}", email);
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
                _logger.LogWarning("Page not initialized for CPS Golf search");
                return slots;
            }

            _logger.LogInformation("CPS Golf: Searching for slots on {Date} at {Time} for {Players} players",
                date.Date, preferredTime, players);

            // Navigate to search page
            var searchUrl = $"{_currentBaseUrl?.TrimEnd('/')}/search-teetime";
            await _page.GotoAsync(searchUrl, new NavigationWaitUntilOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Fill date picker
            var dateInput = await _page.QuerySelectorAsync("input[type='date']");
            if (dateInput != null)
            {
                var dateString = date.ToString("yyyy-MM-dd");
                _logger.LogInformation("Setting date filter to {Date}", dateString);
                await _page.FillAsync("input[type='date']", dateString);
            }

            // Fill time filter
            var timeInput = await _page.QuerySelectorAsync("input[type='time']");
            if (timeInput != null)
            {
                var timeString = preferredTime.ToString(@"hh\:mm");
                _logger.LogInformation("Setting time filter to {Time}", timeString);
                await _page.FillAsync("input[type='time']", timeString);
            }

            // Submit search
            var searchButton = await _page.QuerySelectorAsync("button[type='submit']");
            if (searchButton != null)
            {
                _logger.LogInformation("Submitting CPS Golf search");
                await searchButton.ClickAsync();

                // Wait for results to load
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Parse available slots from results
            var slotElements = await _page.QuerySelectorAllAsync(".tee-time-slot, [data-slot-id], .slot-card");
            _logger.LogInformation("Found {SlotCount} slot elements on CPS Golf search results", slotElements.Count);

            foreach (var element in slotElements)
            {
                try
                {
                    // Extract slot information
                    var slotId = await element.GetAttributeAsync("data-slot-id");
                    var timeText = await element.TextContentAsync();
                    var availableText = await element.GetAttributeAsync("data-available");

                    if (string.IsNullOrEmpty(slotId) || string.IsNullOrEmpty(timeText))
                        continue;

                    // Parse time from text (format varies, typically "10:30 AM" or similar)
                    if (!TryParseTimeFromText(timeText, date, out var slotDateTime))
                        continue;

                    // Check if within window
                    if (!IsWithinTimeWindow(slotDateTime.TimeOfDay, preferredTime, windowMinutes))
                        continue;

                    // Parse available players
                    if (!int.TryParse(availableText, out int availablePlayers))
                        availablePlayers = 4; // Default to 4 if not specified

                    if (availablePlayers < players)
                        continue;

                    var slot = new TeeTimeSlot
                    {
                        SlotId = slotId,
                        DateTime = slotDateTime,
                        AvailablePlayers = availablePlayers,
                        CourseName = "CPS Golf Course"
                    };

                    slots.Add(slot);
                    _logger.LogDebug("Added slot: {SlotId} at {DateTime}", slotId, slotDateTime);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing CPS Golf slot element");
                }
            }

            _logger.LogInformation("CPS Golf search complete: found {SlotCount} matching slots", slots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf search failed");
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
                _logger.LogWarning("Page not initialized for CPS Golf booking");
                return result;
            }

            _logger.LogInformation("CPS Golf: Booking slot {SlotId} at {DateTime} for {Players} players",
                slot.SlotId, slot.DateTime, players);

            // Click the slot to select it
            var slotElement = await _page.QuerySelectorAsync($"[data-slot-id='{slot.SlotId}']");
            if (slotElement == null)
            {
                result.ErrorMessage = "Slot not found on page";
                _logger.LogWarning("Slot {SlotId} not found on CPS Golf page", slot.SlotId);
                return result;
            }

            await slotElement.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Fill player count if applicable
            var playerCountInput = await _page.QuerySelectorAsync("input[name='playerCount'], input[name='players']");
            if (playerCountInput != null)
            {
                _logger.LogInformation("Setting player count to {Players}", players);
                await _page.FillAsync("input[name='playerCount']", players.ToString());
            }

            // Submit booking
            var bookButton = await _page.QuerySelectorAsync("button[type='submit']");
            if (bookButton != null)
            {
                _logger.LogInformation("Submitting CPS Golf booking");
                await bookButton.ClickAsync();
                await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions { Timeout = WaitForNavigationTimeoutMs });
            }

            // Check for confirmation
            var confirmationElement = await _page.QuerySelectorAsync(".confirmation-number, [data-confirmation]");
            if (confirmationElement != null)
            {
                var confirmationText = await confirmationElement.TextContentAsync();
                result.ConfirmationNumber = ExtractConfirmationNumber(confirmationText);
                result.BookedTime = slot.DateTime;
                result.Success = true;
                _logger.LogInformation("CPS Golf booking successful: {ConfirmationNumber}", result.ConfirmationNumber);
            }
            else
            {
                // Check for error message
                var errorElement = await _page.QuerySelectorAsync(".error, .alert-danger, [role='alert']");
                if (errorElement != null)
                {
                    result.ErrorMessage = await errorElement.TextContentAsync();
                }
                else
                {
                    result.ErrorMessage = "Booking may have failed - no confirmation found";
                }
                _logger.LogWarning("CPS Golf booking may have failed: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "CPS Golf booking failed");
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("CPS Golf: Logging out");

            if (_page != null)
            {
                // Look for logout button
                var logoutButton = await _page.QuerySelectorAsync("button[data-logout], a[href*='logout']");
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

            _logger.LogInformation("CPS Golf logout complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CPS Golf logout error");
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
            _logger.LogError(ex, "Error disposing CPS Golf adapter");
        }
    }

    private static bool TryParseTimeFromText(string? text, DateTime date, out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrEmpty(text))
            return false;

        // Try to find time pattern (HH:MM or H:MM with optional AM/PM)
        var timePattern = @"(\d{1,2}):(\d{2})\s*(AM|PM)?";
        var match = System.Text.RegularExpressions.Regex.Match(text, timePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        var hour = int.Parse(match.Groups[1].Value);
        var minute = int.Parse(match.Groups[2].Value);
        var meridiem = match.Groups[3].Value?.ToUpper();

        // Handle 12-hour format
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

        // Look for confirmation number patterns (alphanumeric, typically 6-12 chars)
        var match = System.Text.RegularExpressions.Regex.Match(text, @"[A-Z0-9]{6,12}");
        return match.Success ? match.Value : string.Empty;
    }
}
