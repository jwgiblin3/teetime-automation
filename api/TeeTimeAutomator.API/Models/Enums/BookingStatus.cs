namespace TeeTimeAutomator.API.Models.Enums;

/// <summary>
/// Represents the status of a booking request throughout its lifecycle.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking request has been created but not yet scheduled.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Booking request is scheduled for automatic booking at a future time.
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Automatic booking process is currently in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Booking was successfully completed.
    /// </summary>
    Booked = 3,

    /// <summary>
    /// Booking attempt failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Booking request was cancelled by user.
    /// </summary>
    Cancelled = 5
}
