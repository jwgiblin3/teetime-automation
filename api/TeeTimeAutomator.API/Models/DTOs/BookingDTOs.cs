using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// DTO for creating a booking request.
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// The ID of the golf course to book.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// The desired date for the tee time.
    /// </summary>
    public DateTime DesiredDate { get; set; }

    /// <summary>
    /// The preferred time for the tee time.
    /// </summary>
    public TimeOnly PreferredTime { get; set; }

    /// <summary>
    /// Acceptable window in minutes around preferred time (default: 30).
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Number of players for this tee time.
    /// </summary>
    public int NumberOfPlayers { get; set; }
}

/// <summary>
/// DTO for booking request information.
/// </summary>
public class BookingRequestDto
{
    /// <summary>
    /// The booking request's unique identifier.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The ID of the user who created the request.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the golf course.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Name of the golf course.
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// The desired date for the tee time.
    /// </summary>
    public DateTime DesiredDate { get; set; }

    /// <summary>
    /// The preferred time for the tee time.
    /// </summary>
    public TimeOnly PreferredTime { get; set; }

    /// <summary>
    /// Acceptable window in minutes around preferred time.
    /// </summary>
    public int TimeWindowMinutes { get; set; }

    /// <summary>
    /// Number of players for this tee time.
    /// </summary>
    public int NumberOfPlayers { get; set; }

    /// <summary>
    /// Current status of the booking request.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// When the automatic booking is scheduled to fire.
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The booking result (if available).
    /// </summary>
    public BookingResultDto? BookingResult { get; set; }
}

/// <summary>
/// DTO for booking result information.
/// </summary>
public class BookingResultDto
{
    /// <summary>
    /// The booking result's unique identifier.
    /// </summary>
    public int ResultId { get; set; }

    /// <summary>
    /// The actual booked time.
    /// </summary>
    public DateTime? BookedTime { get; set; }

    /// <summary>
    /// Confirmation number from the booking system.
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Number of booking attempts made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Timestamp of the last booking attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Reason for failure (if applicable).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Whether the booking was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// When the result was created/updated.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for booking status information.
/// </summary>
public class BookingStatusDto
{
    /// <summary>
    /// The booking request's unique identifier.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// Current status of the booking request.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// When the automatic booking is scheduled to fire.
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// The booking result (if available).
    /// </summary>
    public BookingResultDto? BookingResult { get; set; }

    /// <summary>
    /// Status message providing more context.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
