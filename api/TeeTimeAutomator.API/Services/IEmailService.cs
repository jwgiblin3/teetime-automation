namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a booking confirmation email to the user.</summary>
    Task SendBookingConfirmationAsync(string toEmail, string courseName, DateTime bookedTime, string confirmationNumber);

    /// <summary>Sends a booking failure notification email to the user.</summary>
    Task SendBookingFailureAsync(string toEmail, string courseName, DateTime desiredDate, string reason);
}
