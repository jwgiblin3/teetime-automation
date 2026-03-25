using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for managing user profiles.
/// </summary>
public interface IUserService
{
    Task<UserProfileDto?> GetUserByIdAsync(int userId);
    Task<UserProfileDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest request);
    Task<bool> DeleteUserAsync(int userId);
}
