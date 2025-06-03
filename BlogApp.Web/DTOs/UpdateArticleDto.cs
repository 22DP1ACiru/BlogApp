using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.DTOs
{
    public class UpdateArticleDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? Content { get; set; }

        public IFormFile? NewImage { get; set; }

        public bool IsPublished { get; set; }
    }
}