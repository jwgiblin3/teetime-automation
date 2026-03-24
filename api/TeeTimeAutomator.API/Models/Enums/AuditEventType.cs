namespace TeeTimeAutomator.API.Models.Enums;

/// <summary>
/// Represents different types of audit log events.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// User registration event.
    /// </summary>
    UserRegistered = 0,

    /// <summary>
    /// User login event.
    /// </summary>
    UserLogin = 1,

    /// <summary>
    /// User profile update event.
    /// </summary>
    UserProfileUpdated = 2,

    /// <summary>
    /// Booking request created event.
    /// </summary>
    BookingRequestCreated = 3,

    /// <summary>
    /// Booking request scheduled event.
    /// </summary>
    BookingRequestScheduled = 4,

    /// <summary>
    /// Booking attempt started event.
    /// </summary>
    BookingAttemptStarted = 5,

    /// <summary>
    /// Booking completed successfully event.
    /// </summary>
    BookingCompleted = 6,

    /// <summary>
    /// Booking attempt failed event.
    /// </summary>
    BookingFailed = 7,

    /// <summary>
    /// Booking request cancelled event.
    /// </summary>
    BookingCancelled = 8,

    /// <summary>
    /// Course credentials added event.
    /// </summary>
    CourseCredentialAdded = 9,

    /// <summary>
    /// Course credentials updated event.
    /// </summary>
    CourseCredentialUpdated = 10,

    /// <summary>
    /// Course credentials deleted event.
    /// </summary>
    CourseCredentialDeleted = 11,

    /// <summary>
    /// Admin action event.
    /// </summary>
    AdminAction = 12,

    /// <summary>
    /// System error event.
    /// </summary>
    SystemError = 13
}
