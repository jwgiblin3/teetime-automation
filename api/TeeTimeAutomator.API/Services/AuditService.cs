using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of audit service for logging system events.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the AuditService.
    /// </summary>
    public AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Logs an audit event to the database.
    /// </summary>
    public async Task LogEventAsync(int? userId, int? requestId, AuditEventType eventType, string message)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                RequestId = requestId,
                EventType = eventType,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit logged: EventType={EventType}, UserId={UserId}, RequestId={RequestId}, Message={Message}",
                eventType, userId, requestId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event: {Message}", message);
        }
    }
}
