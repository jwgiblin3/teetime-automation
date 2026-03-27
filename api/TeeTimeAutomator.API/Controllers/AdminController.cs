using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Services;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Controllers;

/// <summary>
/// Administrative endpoints (requires IsAdmin role)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IAdminService _adminService;
    private readonly ICourseService _courseService;

    public AdminController(
        ILogger<AdminController> logger,
        IAdminService adminService,
        ICourseService courseService)
    {
        _logger = logger;
        _adminService = adminService;
        _courseService = courseService;
    }

    /// <summary>
    /// List all users with pagination
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters" });

            var result = await _adminService.GetAllUsersAsync(page, pageSize);
            var userDtos = result.Items.Select(MapToAdminUserDto).ToList();

            return Ok(new PagedResult<AdminUserDto>
            {
                Items = userDtos,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllUsers: Error retrieving users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Disable or enable a user account
    /// </summary>
    [HttpPut("users/{id}/disable")]
    public async Task<IActionResult> ToggleUserStatus(int id, [FromBody] ToggleUserStatusRequest request)
    {
        try
        {
            var success = await _adminService.ToggleUserStatusAsync(id, request.IsDisabled);
            if (!success) return NotFound(new { message = "User not found" });

            return Ok(new { message = request.IsDisabled ? "User disabled" : "User enabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ToggleUserStatus: Error toggling user status for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating user status" });
        }
    }

    /// <summary>
    /// Get all golf courses
    /// </summary>
    [HttpGet("courses")]
    public async Task<ActionResult<List<AdminCourseDto>>> GetAllCourses()
    {
        try
        {
            var courses = await _courseService.GetAllCoursesAsync();
            return Ok(courses.Select(MapToAdminCourseDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllCourses: Error retrieving courses");
            return StatusCode(500, new { message = "An error occurred while retrieving courses" });
        }
    }

    /// <summary>
    /// Create a new golf course
    /// </summary>
    [HttpPost("courses")]
    public async Task<ActionResult<AdminCourseDto>> AddCourse([FromBody] CreateCourseRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _courseService.CreateCourseAsync(request);
            _logger.LogInformation("AddCourse: Successfully created course {CourseId}", course.Id);
            return CreatedAtAction(nameof(GetAllCourses), MapToAdminCourseDto(course));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddCourse: Error creating course");
            return StatusCode(500, new { message = "An error occurred while creating the course" });
        }
    }

    /// <summary>
    /// Get all bookings with optional filters
    /// </summary>
    [HttpGet("bookings")]
    public async Task<ActionResult<List<AdminBookingDto>>> GetAllBookings(
        [FromQuery] string? status = null,
        [FromQuery] int? courseId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var bookings = await _adminService.GetAllBookingsAsync(status, courseId, startDate, endDate);
            return Ok(bookings.Select(MapToAdminBookingDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllBookings: Error retrieving bookings");
            return StatusCode(500, new { message = "An error occurred while retrieving bookings" });
        }
    }

    /// <summary>
    /// Get system statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<SystemStatsDto>> GetSystemStats()
    {
        try
        {
            var stats = await _adminService.GetSystemStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSystemStats: Error retrieving statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Get audit logs with pagination
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<PagedResult<AdminAuditLogDto>>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters" });

            var result = await _adminService.GetAuditLogsAsync(page, pageSize);
            var logDtos = result.Items.Select(MapToAuditLogDto).ToList();

            return Ok(new PagedResult<AdminAuditLogDto>
            {
                Items = logDtos,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAuditLogs: Error retrieving audit logs");
            return StatusCode(500, new { message = "An error occurred while retrieving logs" });
        }
    }

    private static AdminUserDto MapToAdminUserDto(User user) => new AdminUserDto
    {
        UserId = user.UserId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        IsAdmin = user.IsAdmin,
        IsActive = user.IsActive,
        HasGoogleOAuth = !string.IsNullOrEmpty(user.GoogleOAuthId),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static AdminCourseDto MapToAdminCourseDto(CourseDto course) => new AdminCourseDto
    {
        Id = int.TryParse(course.Id, out var parsedId) ? parsedId : 0,
        Name = course.Name,
        Platform = course.Platform,
        BookingUrl = course.BookingUrl,
        CreatedAt = course.CreatedAt
    };

    private static AdminBookingDto MapToAdminBookingDto(BookingRequest booking) => new AdminBookingDto
    {
        RequestId = booking.RequestId,
        UserId = booking.UserId,
        UserEmail = booking.User?.Email ?? "Unknown",
        CourseId = booking.CourseId,
        CourseName = booking.Course?.CourseName ?? "Unknown",
        Platform = booking.Course?.Platform ?? default,
        DesiredDate = booking.DesiredDate,
        PreferredTime = booking.PreferredTime,
        NumberOfPlayers = booking.NumberOfPlayers,
        Status = booking.Status,
        ScheduledFireTime = booking.ScheduledFireTime,
        HangfireJobId = booking.HangfireJobId,
        CreatedAt = booking.CreatedAt,
        UpdatedAt = booking.UpdatedAt
    };

    private static AdminAuditLogDto MapToAuditLogDto(AuditLog log) => new AdminAuditLogDto
    {
        Id = log.LogId,
        UserId = log.UserId,
        RequestId = log.RequestId,
        EventType = log.EventType.ToString(),
        Message = log.Message,
        CreatedAt = log.CreatedAt
    };
}

public class AdminCourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string BookingUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? RequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ToggleUserStatusRequest
{
    public bool IsDisabled { get; set; }
}
