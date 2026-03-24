using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents an audit log entry for tracking system events.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry.
    /// </summary>
    public int LogId { get; set; }

    /// <summary>
    /// Foreign key to the user involved in the event (nullable for system events).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Foreign key to the booking request involved (nullable if not applicable).
    /// </summary>
    public int? RequestId { get; set; }

    /// <summary>
    /// The type of event being logged.
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Detailed message describing the event.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event was logged.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// The user involved in this event (if applicable).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// The booking request involved in this event (if applicable).
    /// </summary>
    public BookingRequest? BookingRequest { get; set; }
}
