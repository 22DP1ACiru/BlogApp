using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    /// <summary>
    /// Represents a comment made by a user on an article.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the comment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the content of the comment.
        /// </summary>
        [Required(ErrorMessage = "Comment content cannot be empty.")]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the comment was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the comment was last updated.
        /// Null if the comment has not been updated since creation.
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the Article to which this comment belongs.
        /// </summary>
        [Required]
        public int ArticleId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the ApplicationUser who posted this comment.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        // Navigation properties
        /// <summary>
        /// Navigation property for the commented Article.
        /// </summary>
        [ForeignKey(nameof(ArticleId))]
        public virtual Article Article { get; set; }

        /// <summary>
        /// Navigation property for the commenting User.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the comment is blocked (hidden from public view).
        /// Defaults to false.
        /// </summary>
        public bool IsBlocked { get; set; } = false;
    }
}