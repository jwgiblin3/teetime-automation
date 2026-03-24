using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for logging audit events.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event to the database.
    /// </summary>
    /// <param name="userId">The user involved in the event (optional).</param>
    /// <param name="requestId">The booking request involved (optional).</param>
    /// <param name="eventType">The type of event being logged.</param>
    /// <param name="message">A detailed message about the event.</param>
    Task LogEventAsync(int? userId, int? requestId, AuditEventType eventType, string message);
}
