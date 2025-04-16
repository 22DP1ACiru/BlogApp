using BlogApp.BLL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BlogApp.BLL.Services
{
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        // Inject the logger
        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string recipientEmail, string subject, string htmlMessage)
        {
            // Log the email details instead of sending
            _logger.LogWarning("--- SIMULATED EMAIL ---");
            _logger.LogInformation("To: {Recipient}", recipientEmail);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Body (HTML): {Body}", htmlMessage);
            _logger.LogWarning("--- END SIMULATED EMAIL ---");

            // Simulate successful asynchronous completion
            return Task.CompletedTask;
        }
    }
}