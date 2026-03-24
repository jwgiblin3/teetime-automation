namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// DTO for user registration requests.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password (will be hashed).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation to prevent typos.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number for SMS notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// DTO for user login requests.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO for login responses containing JWT token.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The type of token (typically "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// When the token expires (Unix timestamp).
    /// </summary>
    public long ExpiresIn { get; set; }

    /// <summary>
    /// User information associated with the token.
    /// </summary>
    public UserProfileDto User { get; set; } = null!;
}

/// <summary>
/// DTO for Google OAuth authentication.
/// </summary>
public class GoogleAuthRequest
{
    /// <summary>
    /// The ID token from Google.
    /// </summary>
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional user's phone number for SMS notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
