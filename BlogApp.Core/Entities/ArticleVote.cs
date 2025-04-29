using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    public class ArticleVote
    {
        public int Id { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// The value of the vote (e.g., +1 for upvote, -1 for downvote).
        /// </summary>
        [Required]
        public int VoteValue { get; set; }

        public DateTime VotedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ArticleId))]
        public virtual Article Article { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }
    }
}