using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Services;
using TeeTimeAutomator.API.Models.DTOs;

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
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            _logger.LogInformation("Register: Successfully registered user {Email}", request.Email);
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                User = MapToUserDto(result.User)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register: Error registering user {Email}", request.Email);
            return StatusCode(500, new { message = ex.Message });
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
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            _logger.LogInformation("Login: Successful login for {Email}", request.Email);
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                User = MapToUserDto(result.User)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login: Error during login for {Email}", request.Email);
            return Unauthorized(new { message = ex.Message });
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
                return BadRequest(new { message = "IdToken is required" });

            var result = await _authService.GoogleAuthAsync(request);
            _logger.LogInformation("GoogleLogin: Successful Google authentication");
            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                User = MapToUserDto(result.User)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleLogin: Error during Google authentication");
            return Unauthorized(new { message = ex.Message });
        }
    }

    private static UserDto MapToUserDto(UserProfileDto? profile) => profile == null
        ? new UserDto()
        : new UserDto
        {
            Id = profile.UserId,
            Email = profile.Email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            PhoneNumber = profile.PhoneNumber,
            IsAdmin = profile.IsAdmin
        };
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsAdmin { get; set; }
}
