using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Adapters;

namespace TeeTimeAutomator.API.Controllers;

/// <summary>
/// Golf course management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ILogger<CoursesController> _logger;
    private readonly ICourseService _courseService;
    private readonly IEncryptionService _encryptionService;

    public CoursesController(
        ILogger<CoursesController> logger,
        ICourseService courseService,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _courseService = courseService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Get all available courses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetAllCourses()
    {
        try
        {
            _logger.LogInformation("GetAllCourses: Fetching all courses");
            var courses = await _courseService.GetAllCoursesAsync();
            var courseDtos = courses.Select(MapToCourseDto).ToList();
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
    /// Create a new golf course (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        try
        {
            _logger.LogInformation("CreateCourse: Creating new course {CourseName}", request.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateCourse: Invalid model state for course {CourseName}", request.Name);
                return BadRequest(ModelState);
            }

            var course = await _courseService.CreateCourseAsync(
                request.Name,
                request.Location,
                request.Platform,
                request.BookingUrl,
                request.ReleaseScheduleJson);

            _logger.LogInformation("CreateCourse: Successfully created course {CourseId}", course.Id);
            return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, MapToCourseDto(course));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCourse: Error creating course {CourseName}", request.Name);
            return StatusCode(500, new { message = "An error occurred while creating the course" });
        }
    }

    /// <summary>
    /// Get course details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CourseDto>> GetCourseById(int id)
    {
        try
        {
            _logger.LogInformation("GetCourseById: Fetching course {CourseId}", id);
            var course = await _courseService.GetCourseByIdAsync(id);

            if (course == null)
            {
                _logger.LogWarning("GetCourseById: Course {CourseId} not found", id);
                return NotFound(new { message = "Course not found" });
            }

            return Ok(MapToCourseDto(course));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCourseById: Error retrieving course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the course" });
        }
    }

    /// <summary>
    /// Update an existing course (admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        try
        {
            _logger.LogInformation("UpdateCourse: Updating course {CourseId}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateCourse: Invalid model state for course {CourseId}", id);
                return BadRequest(ModelState);
            }

            var course = await _courseService.UpdateCourseAsync(id, request.Name, request.Location, request.ReleaseScheduleJson);

            if (course == null)
            {
                _logger.LogWarning("UpdateCourse: Course {CourseId} not found", id);
                return NotFound(new { message = "Course not found" });
            }

            _logger.LogInformation("UpdateCourse: Successfully updated course {CourseId}", id);
            return Ok(MapToCourseDto(course));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCourse: Error updating course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the course" });
        }
    }

    /// <summary>
    /// Soft delete a course (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        try
        {
            _logger.LogInformation("DeleteCourse: Deleting course {CourseId}", id);

            var success = await _courseService.DeleteCourseAsync(id);

            if (!success)
            {
                _logger.LogWarning("DeleteCourse: Course {CourseId} not found", id);
                return NotFound(new { message = "Course not found" });
            }

            _logger.LogInformation("DeleteCourse: Successfully deleted course {CourseId}", id);
            return Ok(new { message = "Course deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteCourse: Error deleting course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the course" });
        }
    }

    /// <summary>
    /// Save user credentials for a course
    /// </summary>
    [HttpPost("{id}/credentials")]
    public async Task<IActionResult> SaveCredentials(int id, [FromBody] SaveCredentialsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("SaveCredentials: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("SaveCredentials: Saving credentials for user {UserId} and course {CourseId}", userId, id);

            var success = await _courseService.SaveUserCredentialsAsync(
                userId.Value,
                id,
                request.Email,
                request.Password);

            if (!success)
            {
                _logger.LogWarning("SaveCredentials: Failed to save credentials for user {UserId} and course {CourseId}", userId, id);
                return BadRequest(new { message = "Failed to save credentials" });
            }

            _logger.LogInformation("SaveCredentials: Successfully saved credentials for user {UserId} and course {CourseId}", userId, id);
            return Ok(new { message = "Credentials saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveCredentials: Error saving credentials for course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while saving credentials" });
        }
    }

    /// <summary>
    /// Update user credentials for a course
    /// </summary>
    [HttpPut("{id}/credentials")]
    public async Task<IActionResult> UpdateCredentials(int id, [FromBody] SaveCredentialsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("UpdateCredentials: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("UpdateCredentials: Updating credentials for user {UserId} and course {CourseId}", userId, id);

            var success = await _courseService.UpdateUserCredentialsAsync(
                userId.Value,
                id,
                request.Email,
                request.Password);

            if (!success)
            {
                _logger.LogWarning("UpdateCredentials: Failed to update credentials for user {UserId} and course {CourseId}", userId, id);
                return BadRequest(new { message = "Failed to update credentials" });
            }

            _logger.LogInformation("UpdateCredentials: Successfully updated credentials for user {UserId} and course {CourseId}", userId, id);
            return Ok(new { message = "Credentials updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCredentials: Error updating credentials for course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while updating credentials" });
        }
    }

    /// <summary>
    /// Delete user credentials for a course
    /// </summary>
    [HttpDelete("{id}/credentials")]
    public async Task<IActionResult> DeleteCredentials(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("DeleteCredentials: No user ID in claims");
                return Unauthorized();
            }

            _logger.LogInformation("DeleteCredentials: Deleting credentials for user {UserId} and course {CourseId}", userId, id);

            var success = await _courseService.DeleteUserCredentialsAsync(userId.Value, id);

            if (!success)
            {
                _logger.LogWarning("DeleteCredentials: Failed to delete credentials for user {UserId} and course {CourseId}", userId, id);
                return BadRequest(new { message = "Credentials not found" });
            }

            _logger.LogInformation("DeleteCredentials: Successfully deleted credentials for user {UserId} and course {CourseId}", userId, id);
            return Ok(new { message = "Credentials deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteCredentials: Error deleting credentials for course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting credentials" });
        }
    }

    private CourseDto MapToCourseDto(Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Location = course.Location,
            Platform = course.Platform.ToString(),
            BookingUrl = course.BookingUrl,
            HasCredentials = false // This would be set based on current user's credentials
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
/// Data transfer object for course information
/// </summary>
public class CourseDto
{
    /// <summary>
    /// Course ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Course location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Booking platform
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Booking website URL
    /// </summary>
    public string BookingUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether current user has saved credentials for this course
    /// </summary>
    public bool HasCredentials { get; set; }
}

/// <summary>
/// Request model for creating a course
/// </summary>
public class CreateCourseRequest
{
    /// <summary>
    /// Course name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Course location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Booking platform (CpsGolf, GolfNow, TeeSnap, ForeUp)
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Booking website URL
    /// </summary>
    public string BookingUrl { get; set; } = string.Empty;

    /// <summary>
    /// JSON containing release schedule configuration
    /// </summary>
    public string? ReleaseScheduleJson { get; set; }
}

/// <summary>
/// Request model for updating a course
/// </summary>
public class UpdateCourseRequest
{
    /// <summary>
    /// Course name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Course location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// JSON containing release schedule configuration
    /// </summary>
    public string? ReleaseScheduleJson { get; set; }
}

/// <summary>
/// Request model for saving user credentials
/// </summary>
public class SaveCredentialsRequest
{
    /// <summary>
    /// Email for the booking site account
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password for the booking site account
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
