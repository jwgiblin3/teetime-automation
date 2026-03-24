namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// DTO for user profile information.
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number for notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether the user has admin privileges.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// User account creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User's full name (computed property).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// DTO for updating user profile information.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// User's first name (optional).
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional).
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's phone number (optional).
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Current password to verify identity before updating sensitive information.
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// New password (optional, only if changing password).
    /// </summary>
    public string? NewPassword { get; set; }

    /// <summary>
    /// Confirmation of new password.
    /// </summary>
    public string? ConfirmNewPassword { get; set; }
}
