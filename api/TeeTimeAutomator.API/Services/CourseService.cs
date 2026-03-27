using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of course service.
/// </summary>
public class CourseService : ICourseService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CourseService> _logger;

    /// <summary>
    /// Initializes a new instance of the CourseService.
    /// </summary>
    public CourseService(AppDbContext context, IMapper mapper, IEncryptionService encryptionService,
        IAuditService auditService, ILogger<CourseService> logger)
    {
        _context = context;
        _mapper = mapper;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active courses.
    /// </summary>
    public async Task<List<CourseDto>> GetAllCoursesAsync()
    {
        try
        {
            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .ToListAsync();

            return courses.Select(c => MapCourseToDto(c)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all courses");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific course by ID.
    /// </summary>
    public async Task<CourseDto?> GetCourseByIdAsync(int courseId)
    {
        try
        {
            var course = await _context.Courses.FindAsync(courseId);
            return course != null ? MapCourseToDto(course) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course {CourseId}", courseId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new course.
    /// </summary>
    private static CoursePlatform ParsePlatform(string platformString) => platformString.ToLower() switch
    {
        "cps-golf" => CoursePlatform.CpsGolf,
        "golfnow" => CoursePlatform.GolfNow,
        "teesnap" => CoursePlatform.TeeSnap,
        "foreup" => CoursePlatform.ForeUp,
        _ => CoursePlatform.Other
    };

    public async Task<CourseDto> CreateCourseAsync(CreateCourseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CourseName) || string.IsNullOrWhiteSpace(request.BookingUrl))
            {
                throw new ArgumentException("Course name and booking URL are required");
            }

            var schedule = request.ReleaseSchedule != null
                ? new ReleaseSchedule
                {
                    DaysInAdvance = request.ReleaseSchedule.DaysBeforeRelease,
                    ReleaseTime = $"{request.ReleaseSchedule.ReleaseTimeHour:D2}:{request.ReleaseSchedule.ReleaseTimeMinute:D2}"
                }
                : new ReleaseSchedule();

            var releaseScheduleJson = JsonConvert.SerializeObject(schedule);

            var course = new Course
            {
                CourseName = request.CourseName,
                BookingUrl = request.BookingUrl,
                Platform = ParsePlatform(request.PlatformString),
                ReleaseScheduleJson = releaseScheduleJson,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(null, null, AuditEventType.AdminAction,
                $"Course '{course.CourseName}' created");

            _logger.LogInformation("Course {CourseId} '{CourseName}' created", course.CourseId, course.CourseName);

            return MapCourseToDto(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing course.
    /// </summary>
    public async Task<CourseDto> UpdateCourseAsync(int courseId, UpdateCourseRequest request)
    {
        try
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                throw new InvalidOperationException("Course not found");
            }

            if (!string.IsNullOrWhiteSpace(request.CourseName))
                course.CourseName = request.CourseName;

            if (!string.IsNullOrWhiteSpace(request.BookingUrl))
                course.BookingUrl = request.BookingUrl;

            if (request.Platform.HasValue)
                course.Platform = request.Platform.Value;

            if (request.ReleaseSchedule != null)
                course.ReleaseScheduleJson = JsonConvert.SerializeObject(request.ReleaseSchedule);

            if (request.IsActive.HasValue)
                course.IsActive = request.IsActive.Value;

            course.UpdatedAt = DateTime.UtcNow;

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(null, null, AuditEventType.AdminAction,
                $"Course {courseId} '{course.CourseName}' updated");

            _logger.LogInformation("Course {CourseId} updated", courseId);

            return MapCourseToDto(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId}", courseId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a course (admin only).
    /// </summary>
    public async Task DeleteCourseAsync(int courseId)
    {
        try
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                throw new InvalidOperationException("Course not found");
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(null, null, AuditEventType.AdminAction,
                $"Course {courseId} '{course.CourseName}' deleted");

            _logger.LogInformation("Course {CourseId} deleted", courseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
            throw;
        }
    }

    /// <summary>
    /// Stores encrypted credentials for a user's golf course account.
    /// </summary>
    public async Task StoreCredentialsAsync(int userId, CourseCredentialRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email and password are required");
            }

            var course = await _context.Courses.FindAsync(request.CourseId);
            if (course == null)
            {
                throw new InvalidOperationException("Course not found");
            }

            var existingCredential = await _context.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == request.CourseId);

            var encryptedEmail = _encryptionService.Encrypt(request.Email);
            var encryptedPassword = _encryptionService.Encrypt(request.Password);

            if (existingCredential != null)
            {
                existingCredential.EncryptedEmail = encryptedEmail;
                existingCredential.EncryptedPassword = encryptedPassword;
                existingCredential.UpdatedAt = DateTime.UtcNow;
                _context.UserCourseCredentials.Update(existingCredential);

                await _auditService.LogEventAsync(userId, null, AuditEventType.CourseCredentialUpdated,
                    $"Credentials updated for course {request.CourseId}");

                _logger.LogInformation("Updated credentials for user {UserId} at course {CourseId}",
                    userId, request.CourseId);
            }
            else
            {
                var credential = new UserCourseCredential
                {
                    UserId = userId,
                    CourseId = request.CourseId,
                    EncryptedEmail = encryptedEmail,
                    EncryptedPassword = encryptedPassword,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserCourseCredentials.Add(credential);

                await _auditService.LogEventAsync(userId, null, AuditEventType.CourseCredentialAdded,
                    $"Credentials added for course {request.CourseId}");

                _logger.LogInformation("Stored credentials for user {UserId} at course {CourseId}",
                    userId, request.CourseId);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing credentials for user {UserId} at course {CourseId}",
                userId, request.CourseId);
            throw;
        }
    }

    /// <summary>
    /// Gets a user's credentials for a specific course.
    /// </summary>
    public async Task<(string email, string password)?> GetCredentialsAsync(int userId, int courseId)
    {
        try
        {
            var credential = await _context.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

            if (credential == null)
            {
                return null;
            }

            var email = _encryptionService.Decrypt(credential.EncryptedEmail);
            var password = _encryptionService.Decrypt(credential.EncryptedPassword);

            return (email, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId} at course {CourseId}",
                userId, courseId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a user's credentials for a specific course.
    /// </summary>
    public async Task DeleteCredentialsAsync(int userId, int courseId)
    {
        try
        {
            var credential = await _context.UserCourseCredentials
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

            if (credential != null)
            {
                _context.UserCourseCredentials.Remove(credential);
                await _context.SaveChangesAsync();

                await _auditService.LogEventAsync(userId, null, AuditEventType.CourseCredentialDeleted,
                    $"Credentials deleted for course {courseId}");

                _logger.LogInformation("Deleted credentials for user {UserId} at course {CourseId}",
                    userId, courseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credentials for user {UserId} at course {CourseId}",
                userId, courseId);
            throw;
        }
    }

    /// <summary>
    /// Gets all courses where the user has stored credentials.
    /// </summary>
    public async Task<List<CourseDto>> GetUserCoursesAsync(int userId)
    {
        try
        {
            var courses = await _context.UserCourseCredentials
                .Where(c => c.UserId == userId)
                .Select(c => c.Course)
                .Distinct()
                .ToListAsync();

            return courses.Select(c => MapCourseToDto(c)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses for user {UserId}", userId);
            throw;
        }
    }

    private CourseDto MapCourseToDto(Course course)
    {
        var releaseSchedule = JsonConvert.DeserializeObject<ReleaseSchedule>(course.ReleaseScheduleJson)
                              ?? new ReleaseSchedule();

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            BookingUrl = course.BookingUrl,
            Platform = course.Platform,
            ReleaseSchedule = releaseSchedule,
            IsActive = course.IsActive,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
