using Newtonsoft.Json;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// Release schedule shape sent from the Angular frontend.
/// </summary>
public class CreateReleaseScheduleRequest
{
    public int DaysBeforeRelease { get; set; }
    public int ReleaseTimeHour { get; set; }
    public int ReleaseTimeMinute { get; set; }
}

/// <summary>
/// Represents a tee time release schedule.
/// </summary>
public class ReleaseSchedule
{
    /// <summary>
    /// Number of days in advance that tee times are released.
    /// </summary>
    public int DaysInAdvance { get; set; } = 14;

    /// <summary>
    /// The time of day when tee times are released (HH:mm format, UTC).
    /// </summary>
    public string ReleaseTime { get; set; } = "06:00";
}

/// <summary>
/// DTO for course information.
/// </summary>
public class CourseDto
{
    /// <summary>
    /// Course's unique identifier.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Name of the golf course.
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// URL to the course's booking page.
    /// </summary>
    public string BookingUrl { get; set; } = string.Empty;

    /// <summary>
    /// The booking platform used by the course.
    /// </summary>
    public CoursePlatform Platform { get; set; }

    /// <summary>
    /// The tee time release schedule for this course.
    /// </summary>
    public ReleaseSchedule? ReleaseSchedule { get; set; }

    /// <summary>
    /// Whether the course is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Course creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Course last update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new course. Field names match the Angular frontend payload.
/// </summary>
public class CreateCourseRequest
{
    /// <summary>
    /// Name of the golf course.
    /// </summary>
    [JsonProperty("name")]
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// URL to the course's booking page.
    /// </summary>
    [JsonProperty("bookingUrl")]
    public string BookingUrl { get; set; } = string.Empty;

    /// <summary>
    /// The booking platform string (e.g. "cps-golf", "golfnow").
    /// </summary>
    [JsonProperty("platform")]
    public string PlatformString { get; set; } = string.Empty;

    /// <summary>
    /// The tee time release schedule from the frontend.
    /// </summary>
    [JsonProperty("releaseSchedule")]
    public CreateReleaseScheduleRequest? ReleaseSchedule { get; set; }
}

/// <summary>
/// DTO for updating an existing course.
/// </summary>
public class UpdateCourseRequest
{
    /// <summary>
    /// Name of the golf course (optional).
    /// </summary>
    public string? CourseName { get; set; }

    /// <summary>
    /// URL to the course's booking page (optional).
    /// </summary>
    public string? BookingUrl { get; set; }

    /// <summary>
    /// The booking platform used by the course (optional).
    /// </summary>
    public CoursePlatform? Platform { get; set; }

    /// <summary>
    /// The tee time release schedule for this course (optional).
    /// </summary>
    public ReleaseSchedule? ReleaseSchedule { get; set; }

    /// <summary>
    /// Whether the course is active (optional).
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for storing course credentials.
/// </summary>
public class CourseCredentialRequest
{
    /// <summary>
    /// The course ID to store credentials for.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// The email/username for the course account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The password for the course account.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
