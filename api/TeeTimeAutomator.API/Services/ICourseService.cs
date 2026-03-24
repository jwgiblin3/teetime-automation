using TeeTimeAutomator.API.Models.DTOs;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for managing golf courses and user credentials.
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Gets all active courses.
    /// </summary>
    /// <returns>List of course DTOs.</returns>
    Task<List<CourseDto>> GetAllCoursesAsync();

    /// <summary>
    /// Gets a specific course by ID.
    /// </summary>
    /// <param name="courseId">The course ID.</param>
    /// <returns>Course DTO or null if not found.</returns>
    Task<CourseDto?> GetCourseByIdAsync(int courseId);

    /// <summary>
    /// Creates a new course.
    /// </summary>
    /// <param name="request">Course creation request.</param>
    /// <returns>The created course DTO.</returns>
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request);

    /// <summary>
    /// Updates an existing course.
    /// </summary>
    /// <param name="courseId">The course ID.</param>
    /// <param name="request">Course update request.</param>
    /// <returns>The updated course DTO.</returns>
    Task<CourseDto> UpdateCourseAsync(int courseId, UpdateCourseRequest request);

    /// <summary>
    /// Deletes a course (admin only).
    /// </summary>
    /// <param name="courseId">The course ID.</param>
    Task DeleteCourseAsync(int courseId);

    /// <summary>
    /// Stores encrypted credentials for a user's golf course account.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">Credential request containing course ID, email, and password.</param>
    Task StoreCredentialsAsync(int userId, CourseCredentialRequest request);

    /// <summary>
    /// Gets a user's credentials for a specific course.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="courseId">The course ID.</param>
    /// <returns>Tuple of email and password, or null if not found.</returns>
    Task<(string email, string password)?> GetCredentialsAsync(int userId, int courseId);

    /// <summary>
    /// Deletes a user's credentials for a specific course.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="courseId">The course ID.</param>
    Task DeleteCredentialsAsync(int userId, int courseId);

    /// <summary>
    /// Gets all courses where the user has stored credentials.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of courses with stored credentials.</returns>
    Task<List<CourseDto>> GetUserCoursesAsync(int userId);
}
