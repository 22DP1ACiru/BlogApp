using System;

namespace BlogApp.Web.Models
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string AuthorUsername { get; set; }
        public string? AuthorProfilePictureUrl { get; set; }
        public string AuthorId { get; set; }

        public bool CanModify { get; set; } = false;
    }
}