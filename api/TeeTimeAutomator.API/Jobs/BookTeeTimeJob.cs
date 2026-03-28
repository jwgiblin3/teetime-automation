using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Adapters;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.Enums;
using TeeTimeAutomator.API.Services;

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
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(int bookingRequestId)
    {
        _logger.LogInformation("BookTeeTimeJob: Starting execution for booking request {BookingRequestId}", bookingRequestId);

        BookingRequest? bookingRequest = null;
        Course? course = null;
        IBookingAdapter? adapter = null;

        try
        {
            bookingRequest = await _dbContext.BookingRequests
                .Include(b => b.User)
                .Include(b => b.Course)
                .FirstOrDefaultAsync(b => b.RequestId == bookingRequestId);

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

            var credential = await _dbContext.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == bookingRequest.UserId && c.CourseId == course.CourseId);

            if (credential == null)
            {
                _logger.LogError("No credentials found for user {UserId} and course {CourseId}", bookingRequest.UserId, course.CourseId);
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = "No credentials found for this course";
                await _dbContext.SaveChangesAsync();
                return;
            }

            bookingRequest.Status = BookingStatus.InProgress;
            await _dbContext.SaveChangesAsync();

            adapter = _adapterFactory.CreateAdapter(course.Platform);

            var email = _encryptionService.Decrypt(credential.EncryptedEmail);
            var password = _encryptionService.Decrypt(credential.EncryptedPassword);

            var loginSuccess = await adapter.LoginAsync(course.BookingUrl, email, password);

            if (!loginSuccess)
            {
                _logger.LogWarning("Login failed for booking request {BookingRequestId}", bookingRequestId);
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = "Login failed - invalid credentials or connection error";
                await _dbContext.SaveChangesAsync();

                if (!string.IsNullOrEmpty(bookingRequest.User?.PhoneNumber))
                {
                    try
                    {
                        await _smsService.SendBookingFailureAsync(
                            bookingRequest.User.PhoneNumber,
                            course.CourseName,
                            bookingRequest.DesiredDate,
                            "Login failed");
                    }
                    catch (Exception smsEx) { _logger.LogWarning(smsEx, "Failed to send login-failure SMS for booking {BookingRequestId}", bookingRequestId); }
                }
                return;
            }

            var slots = await adapter.SearchAvailableSlotsAsync(
                bookingRequest.DesiredDate,
                bookingRequest.PreferredTime.ToTimeSpan(),
                bookingRequest.TimeWindowMinutes,
                bookingRequest.NumberOfPlayers);

            _logger.LogInformation("Found {SlotCount} available slots for booking request {BookingRequestId}", slots.Count, bookingRequestId);

            if (slots.Count > 0)
            {
                var selectedSlot = slots
                    .OrderBy(s => Math.Abs((s.DateTime.TimeOfDay - bookingRequest.PreferredTime.ToTimeSpan()).TotalSeconds))
                    .First();

                var bookingResult = await adapter.BookSlotAsync(selectedSlot, bookingRequest.NumberOfPlayers);

                if (bookingResult.Success && !string.IsNullOrEmpty(bookingResult.ConfirmationNumber))
                {
                    _logger.LogInformation("Booking successful for request {BookingRequestId}. Confirmation: {ConfirmationNumber}",
                        bookingRequestId, bookingResult.ConfirmationNumber);

                    var result = new BookingResult
                    {
                        RequestId = bookingRequest.RequestId,
                        IsSuccess = true,
                        ConfirmationNumber = bookingResult.ConfirmationNumber,
                        BookedTime = bookingResult.BookedTime ?? selectedSlot.DateTime,
                        AttemptCount = 1,
                        LastAttemptAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.BookingResults.Add(result);

                    bookingRequest.Status = BookingStatus.Booked;
                    bookingRequest.BookingResult = result;
                    await _dbContext.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(bookingRequest.User?.PhoneNumber))
                    {
                        try
                        {
                            await _smsService.SendBookingConfirmationAsync(
                                bookingRequest.User.PhoneNumber,
                                course.CourseName,
                                bookingResult.BookedTime ?? selectedSlot.DateTime,
                                bookingResult.ConfirmationNumber);
                        }
                        catch (Exception smsEx) { _logger.LogWarning(smsEx, "Failed to send confirmation SMS for booking {BookingRequestId}", bookingRequestId); }
                    }

                    _logger.LogInformation("BookTeeTimeJob completed successfully for request {BookingRequestId}", bookingRequestId);
                }
                else
                {
                    _logger.LogWarning("Booking failed for request {BookingRequestId}: {ErrorMessage}",
                        bookingRequestId, bookingResult.ErrorMessage);

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
                _logger.LogInformation("No available slots for booking request {BookingRequestId}. Enqueueing polling job.", bookingRequestId);
                bookingRequest.Status = BookingStatus.Scheduled;
                bookingRequest.ErrorMessage = "No matching slots available - polling for cancellations";
                await _dbContext.SaveChangesAsync();

                RecurringJob.AddOrUpdate<PollingJob>(
                    $"polling-{bookingRequestId}",
                    job => job.ExecuteAsync(bookingRequestId),
                    Cron.MinuteInterval(5));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BookTeeTimeJob failed for request {BookingRequestId}", bookingRequestId);

            if (bookingRequest != null)
            {
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = $"Exception: {ex.Message}";
                try { await _dbContext.SaveChangesAsync(); }
                catch (Exception saveEx) { _logger.LogError(saveEx, "Failed to save error state for booking request {BookingRequestId}", bookingRequestId); }

                if (bookingRequest.User != null && course != null && !string.IsNullOrEmpty(bookingRequest.User.PhoneNumber))
                {
                    try
                    {
                        await _smsService.SendBookingFailureAsync(
                            bookingRequest.User.PhoneNumber,
                            course.CourseName,
                            bookingRequest.DesiredDate,
                            ex.Message);
                    }
                    catch (Exception smsEx) { _logger.LogError(smsEx, "Failed to send failure SMS for request {BookingRequestId}", bookingRequestId); }
                }
            }

            throw;
        }
        finally
        {
            if (adapter != null)
            {
                try
                {
                    await adapter.LogoutAsync();
                    if (adapter is IAsyncDisposable disposable)
                        await disposable.DisposeAsync();
                }
                catch (Exception ex) { _logger.LogError(ex, "Error during adapter cleanup"); }
            }
        }
    }
}
