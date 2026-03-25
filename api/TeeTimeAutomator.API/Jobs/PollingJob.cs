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
/// Recurring Hangfire job that polls for available tee times every 5 minutes.
/// Stops when booking succeeds, user cancels, or date passes.
/// </summary>
public class PollingJob
{
    private readonly ILogger<PollingJob> _logger;
    private readonly IBookingAdapterFactory _adapterFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly ISmsService _smsService;
    private readonly ICalendarService _calendarService;
    private readonly AppDbContext _dbContext;

    private const int MaxAttempts = 864; // 72 hours at 5-minute intervals

    public PollingJob(
        ILogger<PollingJob> logger,
        IBookingAdapterFactory adapterFactory,
        IEncryptionService encryptionService,
        ISmsService smsService,
        ICalendarService calendarService,
        AppDbContext dbContext)
    {
        _logger = logger;
        _adapterFactory = adapterFactory;
        _encryptionService = encryptionService;
        _smsService = smsService;
        _calendarService = calendarService;
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(int bookingRequestId)
    {
        _logger.LogInformation("PollingJob: Starting execution for booking request {BookingRequestId}", bookingRequestId);

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
                RemovePollingJob(bookingRequestId);
                return;
            }

            course = bookingRequest.Course;
            if (course == null)
            {
                _logger.LogError("Course not found for booking request {BookingRequestId}", bookingRequestId);
                RemovePollingJob(bookingRequestId);
                return;
            }

            if (bookingRequest.Status == BookingStatus.Booked)
            {
                _logger.LogInformation("Booking request {BookingRequestId} already booked, removing polling job", bookingRequestId);
                RemovePollingJob(bookingRequestId);
                return;
            }

            if (bookingRequest.Status == BookingStatus.Cancelled)
            {
                _logger.LogInformation("Booking request {BookingRequestId} was cancelled, removing polling job", bookingRequestId);
                RemovePollingJob(bookingRequestId);
                return;
            }

            if (bookingRequest.DesiredDate.Date < DateTime.UtcNow.Date)
            {
                _logger.LogWarning("Desired date has passed for booking request {BookingRequestId}, removing polling job", bookingRequestId);
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.ErrorMessage = "Desired date has passed";
                await _dbContext.SaveChangesAsync();
                RemovePollingJob(bookingRequestId);
                return;
            }

            var credential = await _dbContext.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == bookingRequest.UserId && c.CourseId == course.CourseId);

            if (credential == null)
            {
                _logger.LogError("No credentials found for polling request {BookingRequestId}", bookingRequestId);
                bookingRequest.Status = BookingStatus.Failed;
                await _dbContext.SaveChangesAsync();
                RemovePollingJob(bookingRequestId);
                return;
            }

            adapter = _adapterFactory.CreateAdapter(course.Platform);

            var email = _encryptionService.Decrypt(credential.EncryptedEmail);
            var password = _encryptionService.Decrypt(credential.EncryptedPassword);

            var loginSuccess = await adapter.LoginAsync(course.BookingUrl, email, password);
            if (!loginSuccess)
            {
                _logger.LogWarning("Login failed during polling for request {BookingRequestId}, will retry next cycle", bookingRequestId);
                return;
            }

            var slots = await adapter.SearchAvailableSlotsAsync(
                bookingRequest.DesiredDate,
                bookingRequest.PreferredTime.ToTimeSpan(),
                bookingRequest.TimeWindowMinutes,
                bookingRequest.NumberOfPlayers);

            _logger.LogInformation("Found {SlotCount} available slots during polling for request {BookingRequestId}", slots.Count, bookingRequestId);

            if (slots.Count > 0)
            {
                var selectedSlot = slots
                    .OrderBy(s => Math.Abs((s.DateTime.TimeOfDay - bookingRequest.PreferredTime.ToTimeSpan()).TotalSeconds))
                    .First();

                var bookingResult = await adapter.BookSlotAsync(selectedSlot, bookingRequest.NumberOfPlayers);

                if (bookingResult.Success && !string.IsNullOrEmpty(bookingResult.ConfirmationNumber))
                {
                    _logger.LogInformation("Polling resulted in successful booking for request {BookingRequestId}. Confirmation: {ConfirmationNumber}",
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
                        await _smsService.SendBookingConfirmationAsync(
                            bookingRequest.User.PhoneNumber,
                            course.CourseName,
                            bookingResult.BookedTime ?? selectedSlot.DateTime,
                            bookingResult.ConfirmationNumber);
                    }

                    RemovePollingJob(bookingRequestId);
                }
                else
                {
                    _logger.LogWarning("Booking failed during polling for request {BookingRequestId}: {ErrorMessage}",
                        bookingRequestId, bookingResult.ErrorMessage);
                }
            }
            else
            {
                _logger.LogInformation("No slots found during polling for request {BookingRequestId}, will retry in 5 minutes", bookingRequestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PollingJob failed for request {BookingRequestId}", bookingRequestId);
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
                catch (Exception ex) { _logger.LogError(ex, "Error during adapter cleanup in PollingJob"); }
            }
        }
    }

    private void RemovePollingJob(int bookingRequestId)
    {
        try
        {
            RecurringJob.RemoveIfExists($"polling-{bookingRequestId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing polling job for booking request {BookingRequestId}", bookingRequestId);
        }
    }
}
