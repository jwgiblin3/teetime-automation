using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents a booking request that will be automatically processed at a scheduled time.
/// </summary>
public class BookingRequest
{
    /// <summary>
    /// Unique identifier for the booking request.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// Foreign key to the user who created this request.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Foreign key to the golf course being booked.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// The desired date for the tee time.
    /// </summary>
    public DateTime DesiredDate { get; set; }

    /// <summary>
    /// The preferred time for the tee time (HH:mm format).
    /// </summary>
    public TimeOnly PreferredTime { get; set; }

    /// <summary>
    /// The acceptable window in minutes around the preferred time (default: 30).
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Number of players for this tee time.
    /// </summary>
    public int NumberOfPlayers { get; set; }

    /// <summary>
    /// Current status of the booking request.
    /// </summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// When the automatic booking should fire (calculated from release schedule).
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// The Hangfire job ID for tracking the scheduled job.
    /// </summary>
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// Timestamp when the booking request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the booking request was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// The user who created this booking request.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The golf course being booked.
    /// </summary>
    public Course Course { get; set; } = null!;

    /// <summary>
    /// The result of this booking request (if completed).
    /// </summary>
    public BookingResult? BookingResult { get; set; }

    /// <summary>
    /// Audit logs related to this booking request.
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
