namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents the result of a completed booking request.
/// </summary>
public class BookingResult
{
    /// <summary>
    /// Unique identifier for the booking result.
    /// </summary>
    public int ResultId { get; set; }

    /// <summary>
    /// Foreign key to the booking request that generated this result.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The actual booked time (may differ from preferred if within time window).
    /// </summary>
    public DateTime? BookedTime { get; set; }

    /// <summary>
    /// Confirmation number provided by the booking system.
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Number of booking attempts made before success or final failure.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Timestamp of the last booking attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Reason for booking failure (if applicable).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Indicates whether the booking was ultimately successful.
    /// </summary>
    public bool IsSuccess { get; set; } = false;

    /// <summary>
    /// Timestamp when the booking result was created/updated.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the booking result was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// The booking request that produced this result.
    /// </summary>
    public BookingRequest BookingRequest { get; set; } = null!;
}
