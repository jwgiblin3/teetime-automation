using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

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
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;
    private readonly IBookingService _bookingService;

    public AdminController(
        ILogger<AdminController> logger,
        IAdminService adminService,
        IUserService userService,
        ICourseService courseService,
        IBookingService bookingService)
    {
        _logger = logger;
        _adminService = adminService;
        _userService = userService;
        _courseService = courseService;
        _bookingService = bookingService;
    }

    /// <summary>
    /// List all users with pagination
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserAdminDto>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("GetAllUsers: Fetching users page {Page} with page size {PageSize}", page, pageSize);

            if (page < 1 || pageSize < 1)
            {
                _logger.LogWarning("GetAllUsers: Invalid pagination parameters");
                return BadRequest(new { message = "Invalid pagination parameters" });
            }

            var result = await _adminService.GetAllUsersAsync(page, pageSize);
            var userDtos = result.Items.Select(MapToUserAdminDto).ToList();

            _logger.LogInformation("GetAllUsers: Retrieved {UserCount} users for page {Page}", userDtos.Count, page);

            return Ok(new PagedResult<UserAdminDto>
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
            _logger.LogInformation("ToggleUserStatus: Toggling status for user {UserId} to {IsDisabled}", id, request.IsDisabled);

            var success = await _adminService.ToggleUserStatusAsync(id, request.IsDisabled);

            if (!success)
            {
                _logger.LogWarning("ToggleUserStatus: User {UserId} not found", id);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("ToggleUserStatus: Successfully updated user {UserId}", id);
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
            _logger.LogInformation("GetAllCourses: Fetching all courses");
            var courses = await _courseService.GetAllCoursesAsync();
            var courseDtos = courses.Select(MapToAdminCourseDto).ToList();
            _logger.LogInformation("GetAllCourses: Retrieved {CourseCount} courses", courseDtos.Count);
            return Ok(courseDtos);
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
            _logger.LogInformation("AddCourse: Creating new course {CourseName}", request.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddCourse: Invalid model state");
                return BadRequest(ModelState);
            }

            var course = await _courseService.CreateCourseAsync(
                request.Name,
                request.Location,
                request.Platform,
                request.BookingUrl,
                request.ReleaseScheduleJson);

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
            _logger.LogInformation("GetAllBookings: Fetching bookings with filters - Status: {Status}, CourseId: {CourseId}",
                status, courseId);

            var bookings = await _adminService.GetAllBookingsAsync(status, courseId, startDate, endDate);
            var dtos = bookings.Select(MapToAdminBookingDto).ToList();

            _logger.LogInformation("GetAllBookings: Retrieved {BookingCount} bookings", dtos.Count);
            return Ok(dtos);
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
            _logger.LogInformation("GetSystemStats: Fetching system statistics");

            var stats = await _adminService.GetSystemStatsAsync();

            _logger.LogInformation("GetSystemStats: Retrieved statistics");
            return Ok(new SystemStatsDto
            {
                TotalUsers = stats.TotalUsers,
                TotalCourses = stats.TotalCourses,
                BookingsToday = stats.BookingsToday,
                SuccessfulBookingsToday = stats.SuccessfulBookingsToday,
                SuccessRatePercent = stats.SuccessRatePercent,
                ActiveBookings = stats.ActiveBookings,
                FailedBookings = stats.FailedBookings
            });
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
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("GetAuditLogs: Fetching audit logs page {Page}", page);

            if (page < 1 || pageSize < 1)
            {
                _logger.LogWarning("GetAuditLogs: Invalid pagination parameters");
                return BadRequest(new { message = "Invalid pagination parameters" });
            }

            var result = await _adminService.GetAuditLogsAsync(page, pageSize);
            var logDtos = result.Items.Select(MapToAuditLogDto).ToList();

            _logger.LogInformation("GetAuditLogs: Retrieved {LogCount} logs for page {Page}", logDtos.Count, page);

            return Ok(new PagedResult<AuditLogDto>
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

    private UserAdminDto MapToUserAdminDto(User user)
    {
        return new UserAdminDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin,
            IsDisabled = user.IsDisabled,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private AdminCourseDto MapToAdminCourseDto(Course course)
    {
        return new AdminCourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Location = course.Location,
            Platform = course.Platform.ToString(),
            BookingUrl = course.BookingUrl,
            CreatedAt = course.CreatedAt
        };
    }

    private AdminBookingDto MapToAdminBookingDto(BookingRequest booking)
    {
        return new AdminBookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserEmail = booking.User?.Email ?? "Unknown",
            CourseId = booking.CourseId,
            CourseName = booking.Course?.Name ?? "Unknown",
            PreferredDate = booking.PreferredDate,
            PreferredTime = booking.PreferredTime,
            Players = booking.Players,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            BookedAt = booking.BookingResult?.BookedAt,
            ConfirmationNumber = booking.BookingResult?.ConfirmationNumber
        };
    }

    private AuditLogDto MapToAuditLogDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action,
            ResourceType = log.ResourceType,
            ResourceId = log.ResourceId,
            Details = log.Details,
            IpAddress = log.IpAddress,
            Timestamp = log.Timestamp
        };
    }
}

/// <summary>
/// Data transfer object for user in admin context
/// </summary>
public class UserAdminDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Data transfer object for course in admin context
/// </summary>
public class AdminCourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string BookingUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for booking in admin context
/// </summary>
public class AdminBookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public TimeSpan PreferredTime { get; set; }
    public int Players { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? BookedAt { get; set; }
    public string? ConfirmationNumber { get; set; }
}

/// <summary>
/// System statistics data transfer object
/// </summary>
public class SystemStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalCourses { get; set; }
    public int BookingsToday { get; set; }
    public int SuccessfulBookingsToday { get; set; }
    public decimal SuccessRatePercent { get; set; }
    public int ActiveBookings { get; set; }
    public int FailedBookings { get; set; }
}

/// <summary>
/// Audit log data transfer object
/// </summary>
public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public int? ResourceId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Generic paged result container
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Request model for toggling user status
/// </summary>
public class ToggleUserStatusRequest
{
    /// <summary>
    /// Whether to disable the user (true = disabled, false = enabled)
    /// </summary>
    public bool IsDisabled { get; set; }
}
