using Intern.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;

namespace Intern.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotification([FromBody] EmailNotificationRequest request)
        {
            try
            {
                _logger.LogInformation($"Received email notification request for {request.To}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for email notification request");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request data",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Validate email address format
                if (!IsValidEmail(request.To))
                {
                    _logger.LogWarning($"Invalid email address format: {request.To}");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid email address format"
                    });
                }

                var result = await _emailService.SendMeetingNotificationAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation($"Email sent successfully to {request.To}");
                    return Ok(new
                    {
                        success = true,
                        message = "Email sent successfully",
                        messageId = result.MessageId
                    });
                }
                else
                {
                    _logger.LogError($"Failed to send email to {request.To}: {result.ErrorMessage}");
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while sending email to {request.To}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while sending the email",
                    error = ex.Message
                });
            }
        }

        [HttpPost("send-credentials")]
        [AllowAnonymous] // Optional: allow sending without auth for testing
        public async Task<IActionResult> SendCredentials([FromBody] SendCredentialsRequest request)
        {
            try
            {
                var result = await _emailService.SendCredentialsAsync(
                    request.To, request.RecipientName, request.Email, request.Password
                );

                if (result.Success)
                {
                    return Ok(new { success = true, message = "Credentials email sent", messageId = result.MessageId });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send credentials email");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var emailRequest = new EmailNotificationRequest
                {
                    To = request.To,
                    Subject = "Test Email from Meeting Room System",
                    RecipientName = "Test User",
                    EventType = "Test",
                    EventDescription = "This is a test email to verify the email service is working correctly.",
                    MeetingTitle = "Test Meeting",
                    MeetingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    MeetingTime = DateTime.Now.ToString("HH:mm"),
                    MeetingDuration = "1 hour",
                    RoomDetails = "Test Room",
                    SentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var result = await _emailService.SendMeetingNotificationAsync(emailRequest);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Test email sent successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to send test email",
                    error = ex.Message
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class EmailNotificationRequest
    {
        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string EventDescription { get; set; } = string.Empty;

        [Required]
        public string MeetingTitle { get; set; } = string.Empty;

        [Required]
        public string MeetingDate { get; set; } = string.Empty;

        [Required]
        public string MeetingTime { get; set; } = string.Empty;

        [Required]
        public string MeetingDuration { get; set; } = string.Empty;

        public string RoomDetails { get; set; } = string.Empty;

        public string SentAt { get; set; } = string.Empty;
    }

    public class TestEmailRequest
    {
        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;
    }

    public class SendCredentialsRequest
    {
        [Required, EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}