using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.DTOs
{
    public class CreateArticleDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? Content { get; set; }

        public IFormFile? Image { get; set; }

        public bool IsPublished { get; set; } = false;
    }
}