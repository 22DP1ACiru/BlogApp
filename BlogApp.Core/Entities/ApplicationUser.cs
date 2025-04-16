using Microsoft.AspNetCore.Identity;

namespace BlogApp.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfilePictureUrl { get; set; }
    }
}