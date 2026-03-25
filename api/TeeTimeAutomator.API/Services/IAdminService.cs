using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Generic paged result container.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Service for administrative operations.
/// </summary>
public interface IAdminService
{
    Task<PagedResult<User>> GetAllUsersAsync(int page, int pageSize);
    Task<bool> ToggleUserStatusAsync(int userId, bool isDisabled);
    Task<List<BookingRequest>> GetAllBookingsAsync(string? status, int? courseId, DateTime? startDate, DateTime? endDate);
    Task<SystemStatsDto> GetSystemStatsAsync();
    Task<PagedResult<AuditLog>> GetAuditLogsAsync(int page, int pageSize);
}
