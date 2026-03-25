using Microsoft.EntityFrameworkCore;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of IUserService using EF Core.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsAdmin = user.IsAdmin,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserProfileDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return new UserProfileDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsAdmin = user.IsAdmin,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
