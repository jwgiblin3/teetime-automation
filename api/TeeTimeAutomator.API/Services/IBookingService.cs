using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for managing tee time booking requests.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Creates a new booking request and schedules it for automatic booking.
    /// </summary>
    /// <param name="userId">The user ID creating the booking.</param>
    /// <param name="request">The booking request details.</param>
    /// <returns>The created booking request DTO.</returns>
    Task<BookingRequestDto> CreateBookingRequestAsync(int userId, CreateBookingRequest request);

    /// <summary>
    /// Gets a specific booking request by ID.
    /// </summary>
    /// <param name="requestId">The booking request ID.</param>
    /// <returns>Booking request DTO or null if not found.</returns>
    Task<BookingRequestDto?> GetBookingRequestAsync(int requestId);

    /// <summary>
    /// Gets all booking requests for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of booking request DTOs.</returns>
    Task<List<BookingRequestDto>> GetUserBookingRequestsAsync(int userId);

    /// <summary>
    /// Gets all booking requests with a specific status.
    /// </summary>
    /// <param name="status">The booking status to filter by.</param>
    /// <returns>List of booking request DTOs.</returns>
    Task<List<BookingRequestDto>> GetBookingRequestsByStatusAsync(int status);

    /// <summary>
    /// Cancels a booking request.
    /// </summary>
    /// <param name="requestId">The booking request ID.</param>
    /// <returns>The updated booking request DTO.</returns>
    Task<BookingRequestDto> CancelBookingRequestAsync(int requestId);

    /// <summary>
    /// Processes a booking request (called by Hangfire job).
    /// </summary>
    /// <param name="requestId">The booking request ID.</param>
    Task ProcessBookingAsync(int requestId);

    /// <summary>
    /// Gets the status of a booking request.
    /// </summary>
    /// <param name="requestId">The booking request ID.</param>
    /// <returns>Booking status DTO.</returns>
    Task<BookingStatusDto?> GetBookingStatusAsync(int requestId);
}
