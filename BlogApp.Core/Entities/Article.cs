using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? Content { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime PublishedDate { get; set; }

        [Required]
        public string AuthorId { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public virtual ApplicationUser Author { get; set; } // Navigation property

        public DateTime? LastUpdatedDate { get; set; }
        
        public bool IsPublished { get; set; } = false;
    }
}