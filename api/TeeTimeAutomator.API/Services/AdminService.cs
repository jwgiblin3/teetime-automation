using Microsoft.EntityFrameworkCore;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of IAdminService using EF Core.
/// </summary>
public class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;

    public AdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<User>> GetAllUsersAsync(int page, int pageSize)
    {
        var query = _dbContext.Users.AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ToggleUserStatusAsync(int userId, bool isDisabled)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        user.IsActive = !isDisabled;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<BookingRequest>> GetAllBookingsAsync(string? status, int? courseId, DateTime? startDate, DateTime? endDate)
    {
        var query = _dbContext.BookingRequests
            .Include(b => b.User)
            .Include(b => b.Course)
            .Include(b => b.BookingResult)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
            query = query.Where(b => b.Status == bookingStatus);

        if (courseId.HasValue)
            query = query.Where(b => b.CourseId == courseId);

        if (startDate.HasValue)
            query = query.Where(b => b.DesiredDate >= startDate);

        if (endDate.HasValue)
            query = query.Where(b => b.DesiredDate <= endDate);

        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    public async Task<SystemStatsDto> GetSystemStatsAsync()
    {
        var totalUsers = await _dbContext.Users.CountAsync();
        var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive);
        var adminCount = await _dbContext.Users.CountAsync(u => u.IsAdmin);
        var totalCourses = await _dbContext.Courses.CountAsync();
        var activeCourses = await _dbContext.Courses.CountAsync(c => c.IsActive);
        var totalBookings = await _dbContext.BookingRequests.CountAsync();
        var pendingBookings = await _dbContext.BookingRequests.CountAsync(b => b.Status == BookingStatus.Pending);
        var scheduledBookings = await _dbContext.BookingRequests.CountAsync(b => b.Status == BookingStatus.Scheduled);
        var inProgressBookings = await _dbContext.BookingRequests.CountAsync(b => b.Status == BookingStatus.InProgress);
        var successfulBookings = await _dbContext.BookingRequests.CountAsync(b => b.Status == BookingStatus.Booked);
        var failedBookings = await _dbContext.BookingRequests.CountAsync(b => b.Status == BookingStatus.Failed);
        decimal successRate = totalBookings > 0 ? (decimal)successfulBookings / totalBookings * 100 : 0;

        return new SystemStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            AdminCount = adminCount,
            TotalCourses = totalCourses,
            ActiveCourses = activeCourses,
            TotalBookingRequests = totalBookings,
            PendingBookingRequests = pendingBookings,
            ScheduledBookingRequests = scheduledBookings,
            InProgressBookingRequests = inProgressBookings,
            SuccessfulBookings = successfulBookings,
            FailedBookings = failedBookings,
            SuccessRate = successRate
        };
    }

    public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(int page, int pageSize)
    {
        var query = _dbContext.AuditLogs.AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
