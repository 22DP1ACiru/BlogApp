using System.ComponentModel.DataAnnotations;

namespace BlogApp.Web.Models
{
    public class EditCommentViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "Comment cannot be empty.")]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }
    }
}