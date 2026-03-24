using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// DTO for admin user information with full details.
/// </summary>
public class AdminUserDto
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number for notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether the user has admin privileges.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user has Google OAuth configured.
    /// </summary>
    public bool HasGoogleOAuth { get; set; }

    /// <summary>
    /// Number of active booking requests.
    /// </summary>
    public int ActiveBookingCount { get; set; }

    /// <summary>
    /// Number of successful bookings.
    /// </summary>
    public int SuccessfulBookingCount { get; set; }

    /// <summary>
    /// Number of failed booking attempts.
    /// </summary>
    public int FailedBookingCount { get; set; }

    /// <summary>
    /// User account creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for admin booking information with full details.
/// </summary>
public class AdminBookingDto
{
    /// <summary>
    /// The booking request's unique identifier.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The ID of the user who created the request.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The email of the user who created the request.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the golf course.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Name of the golf course.
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// The booking platform.
    /// </summary>
    public CoursePlatform Platform { get; set; }

    /// <summary>
    /// The desired date for the tee time.
    /// </summary>
    public DateTime DesiredDate { get; set; }

    /// <summary>
    /// The preferred time for the tee time.
    /// </summary>
    public TimeOnly PreferredTime { get; set; }

    /// <summary>
    /// Number of players.
    /// </summary>
    public int NumberOfPlayers { get; set; }

    /// <summary>
    /// Current status of the booking request.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// When the automatic booking is scheduled to fire.
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// The Hangfire job ID.
    /// </summary>
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// Booking result information.
    /// </summary>
    public BookingResultDto? BookingResult { get; set; }

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for system statistics.
/// </summary>
public class SystemStatsDto
{
    /// <summary>
    /// Total number of registered users.
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of active users.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Number of administrators.
    /// </summary>
    public int AdminCount { get; set; }

    /// <summary>
    /// Total number of courses in the system.
    /// </summary>
    public int TotalCourses { get; set; }

    /// <summary>
    /// Number of active courses.
    /// </summary>
    public int ActiveCourses { get; set; }

    /// <summary>
    /// Total number of booking requests.
    /// </summary>
    public int TotalBookingRequests { get; set; }

    /// <summary>
    /// Number of pending booking requests.
    /// </summary>
    public int PendingBookingRequests { get; set; }

    /// <summary>
    /// Number of scheduled booking requests.
    /// </summary>
    public int ScheduledBookingRequests { get; set; }

    /// <summary>
    /// Number of in-progress booking requests.
    /// </summary>
    public int InProgressBookingRequests { get; set; }

    /// <summary>
    /// Number of successful bookings.
    /// </summary>
    public int SuccessfulBookings { get; set; }

    /// <summary>
    /// Number of failed bookings.
    /// </summary>
    public int FailedBookings { get; set; }

    /// <summary>
    /// Success rate percentage (0-100).
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// Average number of attempts per successful booking.
    /// </summary>
    public decimal AverageAttemptsPerSuccess { get; set; }

    /// <summary>
    /// Current pending Hangfire jobs.
    /// </summary>
    public int PendingHangfireJobs { get; set; }

    /// <summary>
    /// When the stats were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
