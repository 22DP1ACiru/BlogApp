using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.Models
{
    public class EditArticleViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Content { get; set; }

        [Display(Name = "Current Image")]
        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Replace Image")]
        public IFormFile? NewImage { get; set; }

        [Display(Name = "Published")]
        public bool IsPublished { get; set; }
    }
}