namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for sending SMS notifications via Twilio.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a booking confirmation SMS.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="courseName">Name of the golf course.</param>
    /// <param name="bookedTime">Confirmed tee time.</param>
    /// <param name="confirmationNumber">Booking confirmation number.</param>
    Task SendBookingConfirmationAsync(string phoneNumber, string courseName, DateTime bookedTime, string confirmationNumber);

    /// <summary>
    /// Sends a booking failure notification SMS.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="courseName">Name of the golf course.</param>
    /// <param name="desiredDate">The desired tee time date.</param>
    /// <param name="reason">Reason for the failure.</param>
    Task SendBookingFailureAsync(string phoneNumber, string courseName, DateTime desiredDate, string reason);

    /// <summary>
    /// Sends an SMS notification for login from a new device.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="ipAddress">IP address of the login.</param>
    Task SendLoginSecurityNotificationAsync(string phoneNumber, string ipAddress);

    /// <summary>
    /// Sends a general notification SMS.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number.</param>
    /// <param name="message">Message to send.</param>
    Task SendSmsAsync(string phoneNumber, string message);
}
