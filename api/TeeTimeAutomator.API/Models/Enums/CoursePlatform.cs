namespace TeeTimeAutomator.API.Models.Enums;

/// <summary>
/// Represents the booking platform used by a golf course.
/// </summary>
public enum CoursePlatform
{
    /// <summary>
    /// CPS Golf tee time management platform.
    /// </summary>
    CpsGolf = 0,

    /// <summary>
    /// GolfNow booking platform.
    /// </summary>
    GolfNow = 1,

    /// <summary>
    /// TeeSnap booking platform.
    /// </summary>
    TeeSnap = 2,

    /// <summary>
    /// ForeUp tee time management system.
    /// </summary>
    ForeUp = 3,

    /// <summary>
    /// Other/custom booking platform.
    /// </summary>
    Other = 4
}
