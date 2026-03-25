using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Hangfire;
using TeeTimeAutomator.API.Services;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Jobs;

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
            if (userId == null) return Unauthorized();

            _logger.LogInformation("GetMyBookings: Fetching bookings for user {UserId}", userId);
            var bookings = await _bookingService.GetUserBookingRequestsAsync(userId.Value);
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyBookings: Error retrieving user bookings");
            return StatusCode(500, new { message = "An error occurred while retrieving bookings" });
        }
    }

    /// <summary>
    /// Create a new booking request and schedule it
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingRequestDto>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _logger.LogInformation("CreateBooking: Creating booking for user {UserId} at course {CourseId}", userId, request.CourseId);
            var bookingRequest = await _bookingService.CreateBookingRequestAsync(userId.Value, request);

            BackgroundJob.Enqueue<ScheduleBookingJob>(job => job.ExecuteAsync(bookingRequest.RequestId));

            _logger.LogInformation("CreateBooking: Created booking {RequestId} for user {UserId}", bookingRequest.RequestId, userId);
            return CreatedAtAction(nameof(GetBookingById), new { id = bookingRequest.RequestId }, bookingRequest);
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
    public async Task<ActionResult<BookingRequestDto>> GetBookingById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation("GetBookingById: Fetching booking {BookingId} for user {UserId}", id, userId);
            var booking = await _bookingService.GetBookingRequestAsync(id);

            if (booking == null) return NotFound(new { message = "Booking not found" });
            if (booking.UserId != userId) return Forbid();

            return Ok(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBookingById: Error retrieving booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the booking" });
        }
    }

    /// <summary>
    /// Cancel a booking request
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var booking = await _bookingService.GetBookingRequestAsync(id);
            if (booking == null) return NotFound(new { message = "Booking not found" });
            if (booking.UserId != userId) return Forbid();

            RecurringJob.RemoveIfExists($"polling-{id}");

            await _bookingService.CancelBookingRequestAsync(id);

            _logger.LogInformation("CancelBooking: Cancelled booking {BookingId} for user {UserId}", id, userId);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelBooking: Error cancelling booking {BookingId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the booking" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }
}
