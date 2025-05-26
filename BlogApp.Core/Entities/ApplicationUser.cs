using Microsoft.AspNetCore.Identity;

namespace BlogApp.Core.Entities
{
    /// <summary>
    /// Represents an application user, extending the base IdentityUser.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Gets or sets the URL of the user's profile picture.
        /// Can be null if the user has not uploaded a profile picture.
        /// </summary>
        public string? ProfilePictureUrl { get; set; }
    }
}