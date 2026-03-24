using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of SMS service using Twilio.
/// </summary>
public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;
    private readonly bool _enableSmsNotifications;

    /// <summary>
    /// Initializes a new instance of the SmsService.
    /// </summary>
    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _enableSmsNotifications = bool.Parse(_configuration["Features:EnableSmsNotifications"] ?? "false");

        if (_enableSmsNotifications)
        {
            InitializeTwilio();
        }
    }

    /// <summary>
    /// Initializes the Twilio client.
    /// </summary>
    private void InitializeTwilio()
    {
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];

            if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
            {
                _logger.LogWarning("Twilio credentials not configured");
                return;
            }

            TwilioClient.Init(accountSid, authToken);
            _logger.LogInformation("Twilio client initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Twilio client");
        }
    }

    /// <summary>
    /// Sends a booking confirmation SMS.
    /// </summary>
    public async Task SendBookingConfirmationAsync(string phoneNumber, string courseName, DateTime bookedTime, string confirmationNumber)
    {
        try
        {
            if (!_enableSmsNotifications || string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogInformation("SMS notifications disabled or no phone number provided");
                return;
            }

            var message = $"Great news! Your tee time at {courseName} is confirmed for {bookedTime:g}. Confirmation #: {confirmationNumber}";

            await SendSmsAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking confirmation SMS");
            throw;
        }
    }

    /// <summary>
    /// Sends a booking failure notification SMS.
    /// </summary>
    public async Task SendBookingFailureAsync(string phoneNumber, string courseName, DateTime desiredDate, string reason)
    {
        try
        {
            if (!_enableSmsNotifications || string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogInformation("SMS notifications disabled or no phone number provided");
                return;
            }

            var message = $"Unfortunately, booking at {courseName} for {desiredDate:d} failed. Reason: {reason}";

            await SendSmsAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking failure SMS");
            throw;
        }
    }

    /// <summary>
    /// Sends an SMS notification for login from a new device.
    /// </summary>
    public async Task SendLoginSecurityNotificationAsync(string phoneNumber, string ipAddress)
    {
        try
        {
            if (!_enableSmsNotifications || string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogInformation("SMS notifications disabled or no phone number provided");
                return;
            }

            var message = $"Your TeeTimeAutomator account was accessed from IP: {ipAddress}. If this wasn't you, please change your password immediately.";

            await SendSmsAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending login security SMS");
            throw;
        }
    }

    /// <summary>
    /// Sends a general SMS notification.
    /// </summary>
    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            if (!_enableSmsNotifications)
            {
                _logger.LogInformation("SMS notifications are disabled");
                return;
            }

            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Phone number or message is empty");
                return;
            }

            var fromNumber = _configuration["Twilio:FromNumber"];
            if (string.IsNullOrWhiteSpace(fromNumber))
            {
                _logger.LogWarning("Twilio FromNumber not configured");
                return;
            }

            var sms = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(phoneNumber)
            );

            _logger.LogInformation("SMS sent successfully: {MessageSid}", sms.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }
}
