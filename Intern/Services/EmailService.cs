using System.Net.Mail;
using System.Net;
using Intern.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Intern.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _emailSettings = new EmailSettings
            {
                SenderEmail = _configuration["EmailSettings:SenderEmail"],
                SenderName = _configuration["EmailSettings:SenderName"],
                SenderPassword = _configuration["EmailSettings:SenderPassword"],
                SmtpServer = _configuration["EmailSettings:SmtpServer"],
                SmtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"])
            };
        }

        public async Task<EmailResult> SendMeetingNotificationAsync(EmailNotificationRequest request)
        {
            try
            {
                _logger.LogInformation($"Preparing to send email to: {request.To}");
                _logger.LogDebug($"Email request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.To) ||
                    string.IsNullOrWhiteSpace(request.Subject) ||
                    string.IsNullOrWhiteSpace(request.RecipientName))
                {
                    return new EmailResult
                    {
                        Success = false,
                        ErrorMessage = "Missing required fields: To, Subject, or RecipientName"
                    };
                }

                // Create the email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = request.Subject,
                    Body = GenerateEmailBody(request),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(request.To, request.RecipientName));

                // Configure SMTP client
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = _emailSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                // Send the email
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent successfully to: {request.To}");

                return new EmailResult
                {
                    Success = true,
                    MessageId = Guid.NewGuid().ToString() // Generate a unique message ID
                };
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"SMTP error sending email to {request.To}");
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = $"SMTP Error: {smtpEx.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error sending email to {request.To}");
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<EmailResult> SendCredentialsAsync(string to, string recipientName, string email, string password)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Your Taska Login Credentials",
                    Body = GenerateCredentialsEmailBody(recipientName, email, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(to, recipientName));

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = _emailSettings.EnableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);

                return new EmailResult
                {
                    Success = true,
                    MessageId = Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending credentials email");
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<EmailResult> SendVerificationCodeAsync(string to, string recipientName, string code)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Your Verification Code",
                    Body = $"Dear {recipientName},\n\nYour verification code is: {code}\n\nThe code will expire in 10 minutes.\n\nBest regards,\nTaska Admin Team",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(new MailAddress(to, recipientName));

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = _emailSettings.EnableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);

                return new EmailResult
                {
                    Success = true,
                    MessageId = Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code email");
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string GenerateEmailBody(EmailNotificationRequest request)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; margin: -20px -20px 20px -20px; }}
        .content {{ line-height: 1.6; }}
        .meeting-details {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{request.EventType}</h1>
        </div>
        <div class='content'>
            <p>Dear {request.RecipientName},</p>
            
            <p>{request.EventDescription}</p>
            
            <div class='meeting-details'>
                <h3>Meeting Details:</h3>
                <p><strong>Title:</strong> {request.MeetingTitle}</p>
                <p><strong>Date:</strong> {request.MeetingDate}</p>
                <p><strong>Time:</strong> {request.MeetingTime}</p>
                <p><strong>Duration:</strong> {request.MeetingDuration}</p>
                <p><strong>Location:</strong> {request.RoomDetails}</p>
            </div>
            
            <p>Please make sure to attend on time. If you have any questions or need to reschedule, please contact the meeting organizer.</p>
            
            <p>Best regards,<br>
            Meeting Room System</p>
        </div>
        <div class='footer'>
            <p>This email was sent automatically by the Meeting Room System on {request.SentAt}</p>
        </div>
    </div>
</body>
</html>";
        }
        private string GenerateCredentialsEmailBody(string recipientName, string email, string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background-color: #0d47a1; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; margin: -20px -20px 20px -20px; }}
        .content {{ line-height: 1.6; }}
        .credentials-box {{ background-color: #f1f3f5; padding: 15px; border-left: 4px solid #0d47a1; margin: 20px 0; border-radius: 4px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Welcome to Taska</h2>
        </div>
        <div class='content'>
            <p>Dear {recipientName},</p>

            <p>Your account has been created successfully. You can now log in using the credentials below:</p>

            <div class='credentials-box'>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
            </div>

            <p>For security, please make sure to Check your Information after your first login.</p>

            <p>Best regards,<br/>Taska Admin Team</p>
        </div>
        <div class='footer'>
            <p>This email was sent automatically. Do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
        }

    }

    public class EmailSettings
    {
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
    }
}