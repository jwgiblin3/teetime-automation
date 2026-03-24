using Hangfire;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Adapters;

namespace TeeTimeAutomator.API.Jobs;

/// <summary>
/// Hangfire background job for booking tee times
/// </summary>
public class BookTeeTimeJob
{
    private readonly ILogger<BookTeeTimeJob> _logger;
    private readonly IBookingAdapterFactory _adapterFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly IBookingService _bookingService;
    private readonly ICourseService _courseService;
    private readonly ISmsService _smsService;
    private readonly ICalendarService _calendarService;
    private readonly AppDbContext _dbContext;

    public BookTeeTimeJob(
        ILogger<BookTeeTimeJob> logger,
        IBookingAdapterFactory adapterFactory,
        IEncryptionService encryptionService,
        IBookingService bookingService,
        ICourseService courseService,
        ISmsService smsService,
        ICalendarService calendarService,
        AppDbContext dbContext)
    {
        _logger = logger;
        _adapterFactory = adapterFactory;
        _encryptionService = encryptionService;
        _bookingService = bookingService;
        _courseService = courseService;
        _smsService = smsService;
        _calendarService = calendarService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Execute the tee time booking with automatic retries
    /// </summary>
    /// <param name="bookingRequestId">The ID of the booking request to process</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(int bookingRequestId)
    {
        _logger.LogInformation("BookTeeTimeJob: Starting execution for booking request {BookingRequestId}", bookingRequestId);

        BookingRequest? bookingRequest = null;
        Course? course = null;
        UserCourseCredential? credential = null;
        IBookingAdapter? adapter = null;

        try
        {
            // Step 1: Load booking request and related entities
            _logger.LogInformation("Loading booking request {BookingRequestId}", bookingRequestId);
            bookingRequest = await _dbContext.BookingRequests
                .Include(b => b.User)
                .Include(b => b.Course)
                .FirstOrDefaultAsync(b => b.Id == bookingRequestId);

            if (bookingRequest == null)
            {
                _logger.LogError("Booking request {BookingRequestId} not found", bookingRequestId);
                return;
            }

            course = bookingRequest.Course;
            if (course == null)
            {
                _logger.LogError("Course not found for booking request {BookingRequestId}", bookingRequestId);
                bookingRequest.Status = BookingStatus.Failed;
                await _dbContext.SaveChangesAsync();
                return;
            }

            // Load course credentials
            credential = await _dbContext.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == bookingRequest.UserId && c.CourseId == course.Id);

            if (credential == null)
            {
                _logger.LogError("No credentials found for user {UserId} and course {CourseId}", bookingRequest.UserId, course.Id);
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = "No credentials found for this course";
                await _dbContext.SaveChangesAsync();
                return;
            }

            // Step 2: Update status to InProgress
            _logger.LogInformation("Updating booking request {BookingRequestId} status to InProgress", bookingRequestId);
            bookingRequest.Status = BookingStatus.InProgress;
            await _dbContext.SaveChangesAsync();

            // Step 3: Get correct adapter
            _logger.LogInformation("Creating booking adapter for platform {Platform}", course.Platform);
            adapter = _adapterFactory.CreateAdapter(course.Platform);

            // Step 4: Decrypt credentials
            _logger.LogInformation("Decrypting credentials for booking request {BookingRequestId}", bookingRequestId);
            var email = _encryptionService.Decrypt(credential.EncryptedEmail);
            var password = _encryptionService.Decrypt(credential.EncryptedPassword);

            // Step 5: Login to booking site
            _logger.LogInformation("Logging in to {Platform} for user {Email}", course.Platform, email);
            var loginSuccess = await adapter.LoginAsync(course.BookingUrl, email, password);

            if (!loginSuccess)
            {
                _logger.LogWarning("Login failed for booking request {BookingRequestId}", bookingRequestId);
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = "Login failed - invalid credentials or connection error";
                await _dbContext.SaveChangesAsync();
                await _smsService.SendFailureNotificationAsync(
                    bookingRequest.User.PhoneNumber,
                    course.Name,
                    bookingRequest.PreferredDate,
                    "Login failed");
                return;
            }

            // Step 6: Search for available slots
            _logger.LogInformation("Searching for available slots for booking request {BookingRequestId}", bookingRequestId);
            var slots = await adapter.SearchAvailableSlotsAsync(
                bookingRequest.PreferredDate,
                bookingRequest.PreferredTime,
                bookingRequest.TimeWindowMinutes,
                bookingRequest.Players);

            _logger.LogInformation("Found {SlotCount} available slots for booking request {BookingRequestId}", slots.Count, bookingRequestId);

            // Step 7: If slot found - book it
            if (slots.Count > 0)
            {
                _logger.LogInformation("Found {SlotCount} slots, attempting to book first available", slots.Count);

                // Select the best matching slot (closest to preferred time)
                var selectedSlot = slots
                    .OrderBy(s => Math.Abs((s.DateTime.TimeOfDay - bookingRequest.PreferredTime).TotalSeconds))
                    .First();

                _logger.LogInformation("Selected slot {SlotId} at {DateTime} for booking", selectedSlot.SlotId, selectedSlot.DateTime);

                var bookingResult = await adapter.BookSlotAsync(selectedSlot, bookingRequest.Players);

                if (bookingResult.Success && !string.IsNullOrEmpty(bookingResult.ConfirmationNumber))
                {
                    _logger.LogInformation("Booking successful for request {BookingRequestId}. Confirmation: {ConfirmationNumber}",
                        bookingRequestId, bookingResult.ConfirmationNumber);

                    // Save booking result
                    var result = new BookingResult
                    {
                        BookingRequestId = bookingRequest.Id,
                        Success = true,
                        ConfirmationNumber = bookingResult.ConfirmationNumber,
                        BookedDateTime = bookingResult.BookedTime ?? selectedSlot.DateTime,
                        TotalPrice = bookingResult.TotalPrice,
                        BookedAt = DateTime.UtcNow
                    };
                    _dbContext.BookingResults.Add(result);

                    // Update booking request
                    bookingRequest.Status = BookingStatus.Booked;
                    bookingRequest.BookingResult = result;
                    await _dbContext.SaveChangesAsync();

                    // Send SMS confirmation
                    _logger.LogInformation("Sending SMS confirmation for booking {BookingRequestId}", bookingRequestId);
                    await _smsService.SendConfirmationAsync(
                        bookingRequest.User.PhoneNumber,
                        course.Name,
                        bookingResult.BookedTime ?? selectedSlot.DateTime,
                        bookingRequest.Players,
                        bookingResult.ConfirmationNumber);

                    // Generate and send iCal invite
                    _logger.LogInformation("Sending calendar invite for booking {BookingRequestId}", bookingRequestId);
                    var icalContent = _calendarService.GenerateICalEvent(
                        bookingRequest.User.Email,
                        course.Name,
                        bookingResult.BookedTime ?? selectedSlot.DateTime,
                        bookingRequest.Players,
                        bookingResult.ConfirmationNumber);

                    await _calendarService.SendCalendarInviteAsync(
                        bookingRequest.User.Email,
                        course.Name,
                        icalContent);

                    _logger.LogInformation("BookTeeTimeJob completed successfully for request {BookingRequestId}", bookingRequestId);
                }
                else
                {
                    // Booking attempt failed
                    _logger.LogWarning("Booking failed for request {BookingRequestId}: {ErrorMessage}",
                        bookingRequestId, bookingResult.ErrorMessage);

                    // Enqueue polling job to retry later
                    _logger.LogInformation("Enqueueing PollingJob for request {BookingRequestId}", bookingRequestId);
                    bookingRequest.Status = BookingStatus.Scheduled;
                    bookingRequest.ErrorMessage = bookingResult.ErrorMessage ?? "Booking attempt failed";
                    await _dbContext.SaveChangesAsync();

                    RecurringJob.AddOrUpdate<PollingJob>(
                        $"polling-{bookingRequestId}",
                        job => job.ExecuteAsync(bookingRequestId),
                        Cron.MinuteInterval(5));
                }
            }
            else
            {
                // No slots found - schedule polling
                _logger.LogInformation("No available slots found for booking request {BookingRequestId}. Enqueueing polling job.",
                    bookingRequestId);

                bookingRequest.Status = BookingStatus.Scheduled;
                bookingRequest.ErrorMessage = "No matching slots available - polling for cancellations";
                await _dbContext.SaveChangesAsync();

                // Enqueue polling job to check every 5 minutes
                RecurringJob.AddOrUpdate<PollingJob>(
                    $"polling-{bookingRequestId}",
                    job => job.ExecuteAsync(bookingRequestId),
                    Cron.MinuteInterval(5));

                _logger.LogInformation("Polling job enqueued for request {BookingRequestId}", bookingRequestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BookTeeTimeJob failed for request {BookingRequestId}", bookingRequestId);

            if (bookingRequest != null)
            {
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = $"Exception: {ex.Message}";
                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to save error state for booking request {BookingRequestId}", bookingRequestId);
                }

                // Send failure notification
                try
                {
                    if (bookingRequest.User != null && course != null)
                    {
                        await _smsService.SendFailureNotificationAsync(
                            bookingRequest.User.PhoneNumber,
                            course.Name,
                            bookingRequest.PreferredDate,
                            ex.Message);
                    }
                }
                catch (Exception smsEx)
                {
                    _logger.LogError(smsEx, "Failed to send failure notification for booking request {BookingRequestId}", bookingRequestId);
                }
            }

            throw; // Re-throw to allow Hangfire retry logic
        }
        finally
        {
            // Cleanup
            if (adapter != null)
            {
                try
                {
                    _logger.LogInformation("Logging out from booking adapter");
                    await adapter.LogoutAsync();

                    if (adapter is IAsyncDisposable disposable)
                    {
                        await disposable.DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during adapter cleanup");
                }
            }
        }
    }
}
