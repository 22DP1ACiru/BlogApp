using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.Models
{
    public class CreateArticleViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Content { get; set; }

        [Display(Name = "Article Image")]
        public IFormFile? Image { get; set; }

        public string? SubmitAction { get; set; }
    }
}