using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Services;
using TeeTimeAutomator.API.Models.DTOs;

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
            if (userId == null) return Unauthorized();

            _logger.LogInformation("GetMyProfile: Fetching profile for user {UserId}", userId);
            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound(new { message = "User not found" });

            return Ok(user);
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
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _logger.LogInformation("UpdateMyProfile: Updating profile for user {UserId}", userId);
            var user = await _userService.UpdateUserProfileAsync(userId.Value, request);
            if (user == null) return NotFound(new { message = "User not found" });

            return Ok(user);
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
            if (userId == null) return Unauthorized();

            _logger.LogInformation("DeleteMyAccount: Deleting account for user {UserId}", userId);
            var success = await _userService.DeleteUserAsync(userId.Value);
            if (!success) return BadRequest(new { message = "Failed to delete account" });

            return Ok(new { message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteMyAccount: Error deleting user account");
            return StatusCode(500, new { message = "An error occurred while deleting your account" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }
}
