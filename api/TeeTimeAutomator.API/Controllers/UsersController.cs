using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace TeeTimeAutomator.API.Controllers;

/// <summary>
/// User profile and account management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUserService _userService;

    public UsersController(
        ILogger<UsersController> logger,
        IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Get the current authenticated user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("GetMyProfile: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("GetMyProfile: Fetching profile for user {UserId}", userId);
            var user = await _userService.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                _logger.LogWarning("GetMyProfile: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            return Ok(MapToUserProfileDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyProfile: Error retrieving user profile");
            return StatusCode(500, new { message = "An error occurred while retrieving your profile" });
        }
    }

    /// <summary>
    /// Update the current authenticated user's profile
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("UpdateMyProfile: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("UpdateMyProfile: Updating profile for user {UserId}", userId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateMyProfile: Invalid model state for user {UserId}", userId);
                return BadRequest(ModelState);
            }

            var user = await _userService.UpdateUserProfileAsync(
                userId.Value,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            if (user == null)
            {
                _logger.LogWarning("UpdateMyProfile: User {UserId} not found", userId);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("UpdateMyProfile: Successfully updated profile for user {UserId}", userId);
            return Ok(MapToUserProfileDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateMyProfile: Error updating user profile");
            return StatusCode(500, new { message = "An error occurred while updating your profile" });
        }
    }

    /// <summary>
    /// Delete the current authenticated user's account
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("DeleteMyAccount: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("DeleteMyAccount: Deleting account for user {UserId}", userId);

            var success = await _userService.DeleteUserAsync(userId.Value);

            if (!success)
            {
                _logger.LogWarning("DeleteMyAccount: Failed to delete account for user {UserId}", userId);
                return BadRequest(new { message = "Failed to delete account" });
            }

            _logger.LogInformation("DeleteMyAccount: Successfully deleted account for user {UserId}", userId);
            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteMyAccount: Error deleting user account");
            return StatusCode(500, new { message = "An error occurred while deleting your account" });
        }
    }

    private UserProfileDto MapToUserProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Data transfer object for user profile
/// </summary>
public class UserProfileDto
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
    /// Whether user is an admin
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// When the account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateProfileRequest
{
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
