using Intern.Controllers;

namespace Intern.Services
{
    public interface IEmailService
    {
        Task<EmailResult> SendMeetingNotificationAsync(EmailNotificationRequest request);
        Task<EmailResult> SendCredentialsAsync(string to, string recipientName, string email, string password);
        Task<EmailResult> SendVerificationCodeAsync(string to, string recipientName, string code);

    }

    public class EmailResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string MessageId { get; set; }
    }
}