namespace BlogApp.BLL.Interfaces
{
    /// <summary>
    /// Defines a contract for sending emails.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <param name="recipientEmail">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="htmlMessage">The HTML content of the email body.</param>
        Task SendEmailAsync(string recipientEmail, string subject, string htmlMessage);
    }
}