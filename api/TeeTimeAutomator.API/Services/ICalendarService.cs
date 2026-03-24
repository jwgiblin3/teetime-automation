namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for generating calendar files and events.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Generates an iCalendar (.ics) file content for a tee time.
    /// </summary>
    /// <param name="courseName">Name of the golf course.</param>
    /// <param name="teeTime">The tee time date and time.</param>
    /// <param name="confirmationNumber">Booking confirmation number.</param>
    /// <param name="userEmail">User's email address.</param>
    /// <returns>ICS file content as a string.</returns>
    string GenerateICalendarContent(string courseName, DateTime teeTime, string confirmationNumber, string userEmail);

    /// <summary>
    /// Creates a Google Calendar event for a tee time.
    /// </summary>
    /// <param name="accessToken">Google OAuth access token.</param>
    /// <param name="courseName">Name of the golf course.</param>
    /// <param name="teeTime">The tee time date and time.</param>
    /// <param name="confirmationNumber">Booking confirmation number.</param>
    /// <param name="description">Event description.</param>
    /// <returns>Google Calendar event ID.</returns>
    Task<string> CreateGoogleCalendarEventAsync(string accessToken, string courseName, DateTime teeTime,
        string confirmationNumber, string description);
}
