namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents encrypted login credentials for a user's golf course account.
/// </summary>
public class UserCourseCredential
{
    /// <summary>
    /// Unique identifier for the credential record.
    /// </summary>
    public int CredentialId { get; set; }

    /// <summary>
    /// Foreign key to the user who owns these credentials.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Foreign key to the golf course these credentials are for.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Encrypted email/username for the course account.
    /// </summary>
    public string EncryptedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted password for the course account.
    /// </summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the credential was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the credential was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// The user who owns these credentials.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The golf course these credentials are for.
    /// </summary>
    public Course Course { get; set; } = null!;
}
