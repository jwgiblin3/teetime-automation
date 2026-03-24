using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with email and password.
    /// </summary>
    /// <param name="request">Registration request containing user details.</param>
    /// <returns>Login response with JWT token on success.</returns>
    Task<LoginResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request with credentials.</param>
    /// <returns>Login response with JWT token on success.</returns>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Authenticates a user using Google OAuth ID token.
    /// </summary>
    /// <param name="request">Google auth request containing ID token.</param>
    /// <returns>Login response with JWT token on success.</returns>
    Task<LoginResponse> GoogleAuthAsync(GoogleAuthRequest request);

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user ID to generate token for.</param>
    /// <returns>JWT token string.</returns>
    string GenerateJwtToken(int userId);

    /// <summary>
    /// Gets the current authenticated user ID from claims.
    /// </summary>
    /// <param name="principal">The claims principal from the request.</param>
    /// <returns>The user ID or null if not authenticated.</returns>
    int? GetUserIdFromPrincipal(System.Security.Claims.ClaimsPrincipal principal);
}
