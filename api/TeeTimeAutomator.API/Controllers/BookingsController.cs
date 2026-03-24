using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace TeeTimeAutomator.API.Controllers;

/// <summary>
/// Tee time booking request management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ILogger<BookingsController> _logger;
    private readonly IBookingService _bookingService;

    public BookingsController(
        ILogger<BookingsController> logger,
        IBookingService bookingService)
    {
        _logger = logger;
        _bookingService = bookingService;
    }

    /// <summary>
    /// Get all booking requests for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingRequestDto>>> GetMyBookings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetMyBookings: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("GetMyBookings: Fetching bookings for user {UserId}", userId);
            var bookings = await _bookingService.GetUserBookingsAsync(userId.Value);
            var dtos = bookings.Select(MapToBookingRequestDto).ToList();
            _logger.LogInformation("GetMyBookings: Retrieved {BookingCount} bookings for user {UserId}", dtos.Count, userId);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyBookings: Error retrieving user bookings");
            return StatusCode(500, new { message = "An error occurred while retrieving bookings" });
        }
    }

    /// <summary>
    /// Create a new booking request
    /// Triggers ScheduleBookingJob which calculates optimal booking time
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingRequestDto>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("CreateBooking: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("CreateBooking: Creating booking request for user {UserId} at course {CourseId}",
                userId, request.CourseId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateBooking: Invalid model state for user {UserId}", userId);
                return BadRequest(ModelState);
            }

            var bookingRequest = await _bookingService.CreateBookingRequestAsync(
                userId.Value,
                request.CourseId,
                request.PreferredDate,
                request.PreferredTime,
                request.Players,
                request.TimeWindowMinutes);

            if (bookingRequest == null)
            {
                _logger.LogWarning("CreateBooking: Failed to create booking for user {UserId}", userId);
                return BadRequest(new { message = "Failed to create booking request" });
            }

            // Schedule the booking job
            _logger.LogInformation("CreateBooking: Scheduling booking job for request {BookingRequestId}", bookingRequest.Id);
            BackgroundJob.Enqueue<ScheduleBookingJob>(job => job.ExecuteAsync(bookingRequest.Id));

            _logger.LogInformation("CreateBooking: Successfully created booking request {BookingRequestId} for user {UserId}",
                bookingRequest.Id, userId);
            return CreatedAtAction(nameof(GetBookingById), new { id = bookingRequest.Id }, MapToBookingRequestDto(bookingRequest));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateBooking: Error creating booking for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "An error occurred while creating the booking" });
        }
    }

    /// <summary>
    /// Get booking request details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingRequestDetailDto>> GetBookingById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetBookingById: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("GetBookingById: Fetching booking {BookingId} for user {UserId}", id, userId);
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
            {
                _logger.LogWarning("GetBookingById: Booking {BookingId} not found", id);
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.UserId != userId)
            {
                _logger.LogWarning("GetBookingById: User {UserId} not authorized to view booking {BookingId}", userId, id);
                return Forbid();
            }

            return Ok(MapToBookingRequestDetailDto(booking));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBookingById: Error retrieving booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the booking" });
        }
    }

    /// <summary>
    /// Cancel a booking request
    /// Removes the Hangfire job and updates status
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("CancelBooking: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("CancelBooking: Cancelling booking {BookingId} for user {UserId}", id, userId);

            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking == null)
            {
                _logger.LogWarning("CancelBooking: Booking {BookingId} not found", id);
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.UserId != userId)
            {
                _logger.LogWarning("CancelBooking: User {UserId} not authorized to cancel booking {BookingId}", userId, id);
                return Forbid();
            }

            // Remove Hangfire jobs
            if (!string.IsNullOrEmpty(booking.HangfireJobId))
            {
                _logger.LogInformation("CancelBooking: Removing Hangfire job {JobId}", booking.HangfireJobId);
                BackgroundJob.Delete(booking.HangfireJobId);
            }

            // Remove polling job if exists
            var pollingJobId = $"polling-{id}";
            _logger.LogInformation("CancelBooking: Removing polling job {JobId}", pollingJobId);
            RecurringJob.RemoveIfExists(pollingJobId);

            // Update booking status
            var success = await _bookingService.CancelBookingAsync(id);

            if (!success)
            {
                _logger.LogWarning("CancelBooking: Failed to cancel booking {BookingId}", id);
                return BadRequest(new { message = "Failed to cancel booking" });
            }

            _logger.LogInformation("CancelBooking: Successfully cancelled booking {BookingId}", id);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelBooking: Error cancelling booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the booking" });
        }
    }

    private BookingRequestDto MapToBookingRequestDto(BookingRequest request)
    {
        return new BookingRequestDto
        {
            Id = request.Id,
            CourseId = request.CourseId,
            CourseName = request.Course?.Name ?? "Unknown",
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            Players = request.Players,
            Status = request.Status.ToString(),
            CreatedAt = request.CreatedAt,
            ScheduledFireTime = request.ScheduledFireTime
        };
    }

    private BookingRequestDetailDto MapToBookingRequestDetailDto(BookingRequest request)
    {
        return new BookingRequestDetailDto
        {
            Id = request.Id,
            CourseId = request.CourseId,
            CourseName = request.Course?.Name ?? "Unknown",
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            Players = request.Players,
            TimeWindowMinutes = request.TimeWindowMinutes,
            Status = request.Status.ToString(),
            ErrorMessage = request.ErrorMessage,
            CreatedAt = request.CreatedAt,
            ScheduledFireTime = request.ScheduledFireTime,
            Result = request.BookingResult != null ? new BookingResultDto
            {
                Success = request.BookingResult.Success,
                ConfirmationNumber = request.BookingResult.ConfirmationNumber,
                BookedDateTime = request.BookingResult.BookedDateTime,
                TotalPrice = request.BookingResult.TotalPrice,
                BookedAt = request.BookingResult.BookedAt
            } : null
        };
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Data transfer object for booking request
/// </summary>
public class BookingRequestDto
{
    /// <summary>
    /// Booking request ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Course ID
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred tee time date
    /// </summary>
    public DateTime PreferredDate { get; set; }

    /// <summary>
    /// Preferred tee time
    /// </summary>
    public TimeSpan PreferredTime { get; set; }

    /// <summary>
    /// Number of players
    /// </summary>
    public int Players { get; set; }

    /// <summary>
    /// Booking status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the booking request was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the booking job is/was scheduled to run
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }
}

/// <summary>
/// Detailed data transfer object for booking request with result
/// </summary>
public class BookingRequestDetailDto
{
    /// <summary>
    /// Booking request ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Course ID
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred tee time date
    /// </summary>
    public DateTime PreferredDate { get; set; }

    /// <summary>
    /// Preferred tee time
    /// </summary>
    public TimeSpan PreferredTime { get; set; }

    /// <summary>
    /// Number of players
    /// </summary>
    public int Players { get; set; }

    /// <summary>
    /// Time window in minutes around preferred time
    /// </summary>
    public int TimeWindowMinutes { get; set; }

    /// <summary>
    /// Booking status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if booking failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the booking request was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the booking job is/was scheduled to run
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// Booking result if booking was successful
    /// </summary>
    public BookingResultDto? Result { get; set; }
}

/// <summary>
/// Data transfer object for booking result
/// </summary>
public class BookingResultDto
{
    /// <summary>
    /// Whether booking was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Confirmation number from booking site
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Booked tee time
    /// </summary>
    public DateTime? BookedDateTime { get; set; }

    /// <summary>
    /// Total price paid
    /// </summary>
    public decimal? TotalPrice { get; set; }

    /// <summary>
    /// When the booking was completed
    /// </summary>
    public DateTime BookedAt { get; set; }
}

/// <summary>
/// Request model for creating a booking
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// ID of the course to book at
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Desired tee time date
    /// </summary>
    public DateTime PreferredDate { get; set; }

    /// <summary>
    /// Preferred tee time
    /// </summary>
    public TimeSpan PreferredTime { get; set; }

    /// <summary>
    /// Number of players
    /// </summary>
    public int Players { get; set; }

    /// <summary>
    /// Search window in minutes around preferred time (default: 60)
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 60;
}
