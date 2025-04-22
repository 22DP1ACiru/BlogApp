using System.Threading.Tasks;
using BlogApp.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.BLL.Interfaces
{
    public interface IAccountService
    {
        Task<IdentityResult> RegisterUserAsync(ApplicationUser user, string password);
        Task<SignInResult> LoginUserAsync(string email, string password, bool rememberMe);
        Task LogoutUserAsync();
        Task<ApplicationUser?> FindUserByEmailAsync(string email);
        Task<ApplicationUser?> FindUserByIdAsync(string userId);
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
        Task<IdentityResult> UpdateUserProfileAsync(ApplicationUser user);
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token);
        Task<bool> IsEmailConfirmedAsync(ApplicationUser user);
    }
}