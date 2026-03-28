using System.Net;
using System.Net.Mail;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// SMTP-based email notification service.
/// Configure via appsettings.json under "Email": Host, Port, Username, Password, FromAddress, FromName.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _enabled;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _enabled = bool.Parse(_configuration["Features:EnableEmailNotifications"] ?? "false");
    }

    public async Task SendBookingConfirmationAsync(
        string toEmail, string courseName, DateTime bookedTime, string confirmationNumber)
    {
        var subject = $"Tee Time Confirmed – {courseName} on {bookedTime:ddd, MMM d} at {bookedTime:h:mm tt}";
        var body = $"""
            <h2>Your tee time is confirmed!</h2>
            <p><strong>Course:</strong> {courseName}</p>
            <p><strong>Date &amp; Time:</strong> {bookedTime:dddd, MMMM d, yyyy} at {bookedTime:h:mm tt}</p>
            <p><strong>Confirmation #:</strong> <code>{confirmationNumber}</code></p>
            <hr/>
            <p style="color:#888;font-size:12px;">Booked automatically by TeeTime Automator.</p>
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBookingFailureAsync(
        string toEmail, string courseName, DateTime desiredDate, string reason)
    {
        var subject = $"Booking Failed – {courseName} on {desiredDate:ddd, MMM d}";
        var body = $"""
            <h2>Your tee time booking failed.</h2>
            <p><strong>Course:</strong> {courseName}</p>
            <p><strong>Requested Date:</strong> {desiredDate:dddd, MMMM d, yyyy}</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p>You can <a href="#">retry the booking</a> from your TeeTime Automator dashboard.</p>
            <hr/>
            <p style="color:#888;font-size:12px;">Sent by TeeTime Automator.</p>
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email notifications disabled — skipping send to {Email}", toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SendEmailAsync called with empty toEmail");
            return;
        }

        try
        {
            var host     = _configuration["Email:Host"]        ?? throw new InvalidOperationException("Email:Host not configured");
            var port     = int.Parse(_configuration["Email:Port"] ?? "587");
            var username = _configuration["Email:Username"]    ?? "";
            var password = _configuration["Email:Password"]    ?? "";
            var from     = _configuration["Email:FromAddress"] ?? username;
            var fromName = _configuration["Email:FromName"]    ?? "TeeTime Automator";
            var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

            using var client = new SmtpClient(host, port)
            {
                Credentials      = new NetworkCredential(username, password),
                EnableSsl        = enableSsl,
                DeliveryMethod   = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            using var message = new MailMessage
            {
                From       = new MailAddress(from, fromName),
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
