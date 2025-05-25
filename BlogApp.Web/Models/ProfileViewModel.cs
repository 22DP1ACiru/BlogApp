using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Username")]
        public string? Username { get; set; } // Make nullable, remove [Required]

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; } // Make nullable

        [Display(Name = "Profile Picture URL")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "New Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }
    }
}