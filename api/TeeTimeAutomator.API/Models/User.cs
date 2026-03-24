namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents a user of the TeeTimeAutomator system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User's email address (unique).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (BCrypt).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// User's phone number for SMS notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth ID for OAuth authentication.
    /// </summary>
    public string? GoogleOAuthId { get; set; }

    /// <summary>
    /// Indicates whether the user has admin privileges.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Indicates whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the user account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Collection of course credentials associated with this user.
    /// </summary>
    public ICollection<UserCourseCredential> UserCourseCredentials { get; set; } = new List<UserCourseCredential>();

    /// <summary>
    /// Collection of booking requests created by this user.
    /// </summary>
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

    /// <summary>
    /// Collection of audit log entries for this user.
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
