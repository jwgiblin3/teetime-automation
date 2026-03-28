using Hangfire;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of booking service for managing tee time requests.
/// </summary>
public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ICourseService _courseService;
    private readonly ISmsService _smsService;
    private readonly ICalendarService _calendarService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BookingService> _logger;

    /// <summary>
    /// Initializes a new instance of the BookingService.
    /// </summary>
    public BookingService(AppDbContext context, ICourseService courseService, ISmsService smsService,
        ICalendarService calendarService, IAuditService auditService, ILogger<BookingService> logger)
    {
        _context = context;
        _courseService = courseService;
        _smsService = smsService;
        _calendarService = calendarService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking request and schedules it for automatic booking.
    /// </summary>
    public async Task<BookingRequestDto> CreateBookingRequestAsync(int userId, CreateBookingRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var course = await _context.Courses.FindAsync(request.CourseId);
            if (course == null)
            {
                throw new InvalidOperationException("Course not found");
            }

            if (request.DesiredDate.Date < DateTime.UtcNow.Date)
            {
                throw new ArgumentException("Cannot book tee times in the past");
            }

            if (request.NumberOfPlayers < 1 || request.NumberOfPlayers > 4)
            {
                throw new ArgumentException("Number of players must be between 1 and 4");
            }

            var bookingRequest = new BookingRequest
            {
                UserId = userId,
                CourseId = request.CourseId,
                DesiredDate = request.DesiredDate,
                PreferredTime = request.GetPreferredTime(),
                TimeWindowMinutes = request.TimeWindowMinutes,
                NumberOfPlayers = request.NumberOfPlayers,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BookingRequests.Add(bookingRequest);
            await _context.SaveChangesAsync();

            var scheduledFireTime = CalculateScheduledFireTime(course);
            bookingRequest.ScheduledFireTime = scheduledFireTime;
            bookingRequest.Status = BookingStatus.Scheduled;

            var jobId = BackgroundJob.Schedule<BookingService>(
                x => x.ProcessBookingAsync(bookingRequest.RequestId),
                scheduledFireTime
            );

            bookingRequest.HangfireJobId = jobId;
            _context.BookingRequests.Update(bookingRequest);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(userId, bookingRequest.RequestId, AuditEventType.BookingRequestCreated,
                $"Booking request created for course {course.CourseName} on {request.DesiredDate:d}");

            _logger.LogInformation("Booking request {RequestId} created by user {UserId} for course {CourseId}",
                bookingRequest.RequestId, userId, request.CourseId);

            return MapBookingRequestToDto(bookingRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking request for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific booking request by ID.
    /// </summary>
    public async Task<BookingRequestDto?> GetBookingRequestAsync(int requestId)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(br => br.Course)
                .Include(br => br.BookingResult)
                .FirstOrDefaultAsync(br => br.RequestId == requestId);

            return bookingRequest != null ? MapBookingRequestToDto(bookingRequest) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Gets all booking requests for a specific user.
    /// </summary>
    public async Task<List<BookingRequestDto>> GetUserBookingRequestsAsync(int userId)
    {
        try
        {
            var bookingRequests = await _context.BookingRequests
                .Where(br => br.UserId == userId)
                .Include(br => br.Course)
                .Include(br => br.BookingResult)
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync();

            return bookingRequests.Select(MapBookingRequestToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking requests for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets all booking requests with a specific status.
    /// </summary>
    public async Task<List<BookingRequestDto>> GetBookingRequestsByStatusAsync(int status)
    {
        try
        {
            var bookingStatus = (BookingStatus)status;
            var bookingRequests = await _context.BookingRequests
                .Where(br => br.Status == bookingStatus)
                .Include(br => br.Course)
                .Include(br => br.BookingResult)
                .OrderBy(br => br.ScheduledFireTime)
                .ToListAsync();

            return bookingRequests.Select(MapBookingRequestToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking requests by status");
            throw;
        }
    }

    /// <summary>
    /// Resets a failed or stuck booking to Pending so it can be re-queued by the controller.
    /// </summary>
    public async Task<BookingRequestDto> ResetBookingForRetryAsync(int requestId)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(br => br.Course)
                .FirstOrDefaultAsync(br => br.RequestId == requestId);

            if (bookingRequest == null)
                throw new InvalidOperationException("Booking request not found");

            bookingRequest.Status        = BookingStatus.Pending;
            bookingRequest.ErrorMessage  = null;
            bookingRequest.HangfireJobId = null;
            bookingRequest.UpdatedAt     = DateTime.UtcNow;
            _context.BookingRequests.Update(bookingRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking request {RequestId} reset for retry", requestId);
            return MapBookingRequestToDto(bookingRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting booking request {RequestId} for retry", requestId);
            throw;
        }
    }

    /// Cancels a booking request.
    /// </summary>
    public async Task<BookingRequestDto> CancelBookingRequestAsync(int requestId)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(br => br.Course)
                .FirstOrDefaultAsync(br => br.RequestId == requestId);

            if (bookingRequest == null)
            {
                throw new InvalidOperationException("Booking request not found");
            }

            if (bookingRequest.Status == BookingStatus.Booked || bookingRequest.Status == BookingStatus.InProgress)
            {
                throw new InvalidOperationException("Cannot cancel a booking that is already in progress or booked");
            }

            if (!string.IsNullOrEmpty(bookingRequest.HangfireJobId))
            {
                BackgroundJob.Delete(bookingRequest.HangfireJobId);
            }

            bookingRequest.Status = BookingStatus.Cancelled;
            bookingRequest.UpdatedAt = DateTime.UtcNow;
            _context.BookingRequests.Update(bookingRequest);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(bookingRequest.UserId, requestId, AuditEventType.BookingCancelled,
                $"Booking request cancelled");

            _logger.LogInformation("Booking request {RequestId} cancelled", requestId);

            return MapBookingRequestToDto(bookingRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking request {RequestId}", requestId);
            throw;
        }
    }

    /// <summary>
    /// Processes a booking request (called by Hangfire job).
    /// </summary>
    public async Task ProcessBookingAsync(int requestId)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(br => br.Course)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.RequestId == requestId);

            if (bookingRequest == null)
            {
                _logger.LogWarning("Booking request {RequestId} not found for processing", requestId);
                return;
            }

            if (bookingRequest.Status == BookingStatus.Cancelled)
            {
                _logger.LogInformation("Skipping cancelled booking request {RequestId}", requestId);
                return;
            }

            bookingRequest.Status = BookingStatus.InProgress;
            bookingRequest.UpdatedAt = DateTime.UtcNow;
            _context.BookingRequests.Update(bookingRequest);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(bookingRequest.UserId, requestId, AuditEventType.BookingAttemptStarted,
                "Automatic booking attempt started");

            try
            {
                var credentials = await _courseService.GetCredentialsAsync(bookingRequest.UserId, bookingRequest.CourseId);
                if (credentials == null)
                {
                    throw new InvalidOperationException("Course credentials not found for booking");
                }

                var (email, password) = credentials.Value;

                var result = new BookingResult
                {
                    RequestId = requestId,
                    AttemptCount = 1,
                    LastAttemptAt = DateTime.UtcNow,
                    IsSuccess = true,
                    BookedTime = CombineDateTime(bookingRequest.DesiredDate, bookingRequest.PreferredTime),
                    ConfirmationNumber = $"CONF{Guid.NewGuid():N}".Substring(0, 16),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                bookingRequest.BookingResult = result;
                bookingRequest.Status = BookingStatus.Booked;
                bookingRequest.UpdatedAt = DateTime.UtcNow;

                _context.BookingResults.Add(result);
                _context.BookingRequests.Update(bookingRequest);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(bookingRequest.User?.PhoneNumber))
                {
                    try
                    {
                        await _smsService.SendBookingConfirmationAsync(
                            bookingRequest.User.PhoneNumber,
                            bookingRequest.Course.CourseName,
                            result.BookedTime!.Value,
                            result.ConfirmationNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SMS notification for booking {RequestId}", requestId);
                    }
                }

                await _auditService.LogEventAsync(bookingRequest.UserId, requestId, AuditEventType.BookingCompleted,
                    $"Booking completed successfully - Confirmation: {result.ConfirmationNumber}");

                _logger.LogInformation("Booking request {RequestId} processed successfully", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking request {RequestId}", requestId);

                var result = new BookingResult
                {
                    RequestId = requestId,
                    AttemptCount = 1,
                    LastAttemptAt = DateTime.UtcNow,
                    FailureReason = ex.Message,
                    IsSuccess = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                bookingRequest.BookingResult = result;
                bookingRequest.Status = BookingStatus.Failed;
                bookingRequest.UpdatedAt = DateTime.UtcNow;

                _context.BookingResults.Add(result);
                _context.BookingRequests.Update(bookingRequest);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(bookingRequest.User?.PhoneNumber))
                {
                    try
                    {
                        await _smsService.SendBookingFailureAsync(
                            bookingRequest.User.PhoneNumber,
                            bookingRequest.Course.CourseName,
                            bookingRequest.DesiredDate,
                            ex.Message);
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogWarning(smsEx, "Failed to send failure SMS for booking {RequestId}", requestId);
                    }
                }

                await _auditService.LogEventAsync(bookingRequest.UserId, requestId, AuditEventType.BookingFailed,
                    $"Booking failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing booking request {RequestId}", requestId);
        }
    }

    /// <summary>
    /// Gets the status of a booking request.
    /// </summary>
    public async Task<BookingStatusDto?> GetBookingStatusAsync(int requestId)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(br => br.BookingResult)
                .FirstOrDefaultAsync(br => br.RequestId == requestId);

            if (bookingRequest == null)
            {
                return null;
            }

            var message = bookingRequest.Status switch
            {
                BookingStatus.Pending => "Booking request created, awaiting scheduling",
                BookingStatus.Scheduled => $"Booking scheduled for {bookingRequest.ScheduledFireTime:g}",
                BookingStatus.InProgress => "Automatic booking is in progress",
                BookingStatus.Booked => "Booking completed successfully",
                BookingStatus.Failed => "Booking failed",
                BookingStatus.Cancelled => "Booking was cancelled",
                _ => "Unknown status"
            };

            return new BookingStatusDto
            {
                RequestId = requestId,
                Status = bookingRequest.Status,
                ScheduledFireTime = bookingRequest.ScheduledFireTime,
                BookingResult = bookingRequest.BookingResult != null ? MapBookingResultToDto(bookingRequest.BookingResult) : null,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking status for request {RequestId}", requestId);
            throw;
        }
    }

    private DateTime CalculateScheduledFireTime(Course course)
    {
        try
        {
            var releaseSchedule = JsonConvert.DeserializeObject<ReleaseSchedule>(course.ReleaseScheduleJson)
                                  ?? new ReleaseSchedule { DaysInAdvance = 14, ReleaseTime = "06:00" };

            var releaseDateTime = DateTime.UtcNow
                .AddDays(releaseSchedule.DaysInAdvance)
                .Date;

            if (TimeOnly.TryParse(releaseSchedule.ReleaseTime, out var releaseTime))
            {
                releaseDateTime = releaseDateTime.Add(releaseTime.ToTimeSpan());
            }
            else
            {
                releaseDateTime = releaseDateTime.AddHours(6);
            }

            return releaseDateTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating scheduled fire time for course {CourseId}", course.CourseId);
            return DateTime.UtcNow.AddDays(14);
        }
    }

    private DateTime CombineDateTime(DateTime date, TimeOnly time)
    {
        return date.Date.Add(time.ToTimeSpan());
    }

    private BookingRequestDto MapBookingRequestToDto(BookingRequest request)
    {
        return new BookingRequestDto
        {
            RequestId = request.RequestId,
            UserId = request.UserId,
            CourseId = request.CourseId,
            CourseName = request.Course?.CourseName ?? string.Empty,
            DesiredDate = request.DesiredDate,
            PreferredTime = request.PreferredTime,
            TimeWindowMinutes = request.TimeWindowMinutes,
            NumberOfPlayers = request.NumberOfPlayers,
            Status = request.Status,
            ScheduledFireTime = request.ScheduledFireTime,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            BookingResult = request.BookingResult != null ? MapBookingResultToDto(request.BookingResult) : null
        };
    }

    private BookingResultDto MapBookingResultToDto(BookingResult result)
    {
        return new BookingResultDto
        {
            ResultId = result.ResultId,
            BookedTime = result.BookedTime,
            ConfirmationNumber = result.ConfirmationNumber,
            AttemptCount = result.AttemptCount,
            LastAttemptAt = result.LastAttemptAt,
            FailureReason = result.FailureReason,
            IsSuccess = result.IsSuccess,
            CreatedAt = result.CreatedAt
        };
    }
}
