using System.Threading.Tasks;

namespace BlogApp.BLL.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipientEmail, string subject, string htmlMessage);
    }
}