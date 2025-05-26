using BlogApp.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlogApp.BLL.Services
{
    /// <summary>
    /// An implementation of IEmailSender that logs email details to the console
    /// instead of actually sending an email. Used for development and testing.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string recipientEmail, string subject, string htmlMessage)
        {
            // Log the email details instead of sending an actual email.
            _logger.LogWarning("--- SIMULATED EMAIL (LoggingEmailSender) ---");
            _logger.LogInformation("To: {RecipientEmail}", recipientEmail);
            _logger.LogInformation("Subject: {EmailSubject}", subject);
            _logger.LogInformation("Body (HTML): {EmailBody}", htmlMessage);
            _logger.LogWarning("--- END SIMULATED EMAIL ---");

            // Simulate successful asynchronous completion.
            return Task.CompletedTask;
        }
    }
}