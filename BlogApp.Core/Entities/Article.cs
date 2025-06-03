using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    /// <summary>
    /// Represents a blog article.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// Gets or sets the unique identifier for the article.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the article.
        /// </summary>
        [Required(ErrorMessage = "Article title is required.")]
        [StringLength(200, ErrorMessage = "Article title cannot exceed 200 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the main content of the article.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the URL for the article's primary image.
        /// </summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the article was originally published or created.
        /// </summary>
        public DateTime PublishedDate { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the ApplicationUser who authored this article.
        /// </summary>
        [Required]
        public string AuthorId { get; set; }

        /// <summary>
        /// Navigation property for the author of the article.
        /// </summary>
        [ForeignKey(nameof(AuthorId))]
        public virtual ApplicationUser Author { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the article was last updated.
        /// Null if never updated after initial publication.
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the article is published and visible to the public.
        /// Defaults to false (draft).
        /// </summary>
        public bool IsPublished { get; set; } = false;
    }
}