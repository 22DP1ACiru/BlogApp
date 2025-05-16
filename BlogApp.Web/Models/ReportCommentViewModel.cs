using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.Models
{
    public class ReportCommentViewModel
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "Reason for Reporting (Optional)")]
        public string? Reason { get; set; }
    }
}