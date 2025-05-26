using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    /// <summary>
    /// Represents a user's vote on an article.
    /// </summary>
    public class ArticleVote
    {
        /// <summary>
        /// Gets or sets the unique identifier for the vote.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the Article being voted on.
        /// </summary>
        [Required]
        public int ArticleId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the ApplicationUser who cast the vote.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the value of the vote (e.g., +1 for upvote, -1 for downvote).
        /// </summary>
        [Required]
        [Range(-1, 1, ErrorMessage = "Vote value must be 1 (upvote) or -1 (downvote).")]
        public int VoteValue { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the vote was cast or last updated.
        /// </summary>
        public DateTime VotedDate { get; set; }

        // Navigation properties
        /// <summary>
        /// Navigation property for the voted Article.
        /// </summary>
        [ForeignKey(nameof(ArticleId))]
        public virtual Article Article { get; set; }

        /// <summary>
        /// Navigation property for the voting User.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }
    }
}