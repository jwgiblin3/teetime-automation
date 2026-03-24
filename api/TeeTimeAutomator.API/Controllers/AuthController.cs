using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(
        ILogger<AuthController> logger,
        IAuthService authService,
        IConfiguration configuration)
    {
        _logger = logger;
        _authService = authService;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Register: New registration request for {Email}", request.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Register: Invalid model state for email {Email}", request.Email);
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            if (!result.Success)
            {
                _logger.LogWarning("Register: Registration failed for {Email}: {ErrorMessage}", request.Email, result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("Register: Successfully registered user {Email}", request.Email);
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                User = result.User
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register: Unexpected error registering user {Email}", request.Email);
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login: Login attempt for {Email}", request.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login: Invalid model state for email {Email}", request.Email);
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Login: Failed login attempt for {Email}", request.Email);
                return Unauthorized(new { message = result.ErrorMessage ?? "Invalid credentials" });
            }

            _logger.LogInformation("Login: Successful login for {Email}", request.Email);
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                User = result.User
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login: Unexpected error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Login with Google OAuth token
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleAuthRequest request)
    {
        try
        {
            _logger.LogInformation("GoogleLogin: Google OAuth request received");

            if (string.IsNullOrEmpty(request.IdToken))
            {
                _logger.LogWarning("GoogleLogin: Missing IdToken");
                return BadRequest(new { message = "IdToken is required" });
            }

            var result = await _authService.GoogleAuthAsync(request.IdToken);

            if (!result.Success)
            {
                _logger.LogWarning("GoogleLogin: Google authentication failed: {ErrorMessage}", result.ErrorMessage);
                return Unauthorized(new { message = result.ErrorMessage ?? "Google authentication failed" });
            }

            _logger.LogInformation("GoogleLogin: Successful Google authentication");
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                User = result.User
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleLogin: Unexpected error during Google authentication");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Refresh an expired access token using a refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("RefreshToken: Token refresh request received");

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("RefreshToken: Missing refresh token");
                return BadRequest(new { message = "Refresh token is required" });
            }

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
            {
                _logger.LogWarning("RefreshToken: Token refresh failed: {ErrorMessage}", result.ErrorMessage);
                return Unauthorized(new { message = result.ErrorMessage ?? "Token refresh failed" });
            }

            _logger.LogInformation("RefreshToken: Token successfully refreshed");
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                User = result.User
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RefreshToken: Unexpected error refreshing token");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }
}

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User phone number
    /// </summary>
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Request model for email/password login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request model for Google OAuth login
/// </summary>
public class GoogleAuthRequest
{
    /// <summary>
    /// Google ID token from OAuth 2.0 authentication
    /// </summary>
    public string IdToken { get; set; } = string.Empty;
}

/// <summary>
/// Request model for token refresh
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token issued during login
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response model for authentication endpoints
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Whether authentication was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// JWT access token for subsequent authenticated requests
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public UserDto? User { get; set; }
}

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether user account is admin
    /// </summary>
    public bool IsAdmin { get; set; }
}
