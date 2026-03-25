using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models;

/// <summary>
/// Represents a golf course that can be booked through the TeeTimeAutomator system.
/// </summary>
public class Course
{
    /// <summary>
    /// Unique identifier for the course.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Name of the golf course.
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Physical location or address of the golf course.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// URL to the course's booking page.
    /// </summary>
    public string BookingUrl { get; set; } = string.Empty;

    /// <summary>
    /// The booking platform used by this course.
    /// </summary>
    public CoursePlatform Platform { get; set; }

    /// <summary>
    /// JSON-serialized release schedule for tee time availability.
    /// Format: {"daysInAdvance": 14, "releaseTime": "06:00"}
    /// </summary>
    public string ReleaseScheduleJson { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the course is active and accepting bookings.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the course was added to the system.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the course information was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Collection of user credentials for this course.
    /// </summary>
    public ICollection<UserCourseCredential> UserCourseCredentials { get; set; } = new List<UserCourseCredential>();

    /// <summary>
    /// Collection of booking requests for this course.
    /// </summary>
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
}
