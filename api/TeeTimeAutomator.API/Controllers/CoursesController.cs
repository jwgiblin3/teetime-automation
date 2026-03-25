using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using TeeTimeAutomator.API.Services;
using TeeTimeAutomator.API.Models.DTOs;

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

    public CoursesController(
        ILogger<CoursesController> logger,
        ICourseService courseService)
    {
        _logger = logger;
        _courseService = courseService;
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
            return Ok(courses);
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
            _logger.LogInformation("CreateCourse: Creating new course {CourseName}", request.CourseName);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _courseService.CreateCourseAsync(request);
            _logger.LogInformation("CreateCourse: Successfully created course {CourseId}", course.CourseId);
            return CreatedAtAction(nameof(GetCourseById), new { id = course.CourseId }, course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCourse: Error creating course {CourseName}", request.CourseName);
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
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null) return NotFound(new { message = "Course not found" });
            return Ok(course);
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _courseService.UpdateCourseAsync(id, request);
            _logger.LogInformation("UpdateCourse: Successfully updated course {CourseId}", id);
            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCourse: Error updating course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the course" });
        }
    }

    /// <summary>
    /// Delete a course (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        try
        {
            await _courseService.DeleteCourseAsync(id);
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
            if (userId == null) return Unauthorized();

            _logger.LogInformation("SaveCredentials: Saving credentials for user {UserId} and course {CourseId}", userId, id);
            await _courseService.StoreCredentialsAsync(userId.Value, new CourseCredentialRequest
            {
                CourseId = id,
                Email = request.Email,
                Password = request.Password
            });
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
            if (userId == null) return Unauthorized();

            await _courseService.StoreCredentialsAsync(userId.Value, new CourseCredentialRequest
            {
                CourseId = id,
                Email = request.Email,
                Password = request.Password
            });
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
            if (userId == null) return Unauthorized();

            await _courseService.DeleteCredentialsAsync(userId.Value, id);
            return Ok(new { message = "Credentials deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteCredentials: Error deleting credentials for course {CourseId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting credentials" });
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

public class SaveCredentialsRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
