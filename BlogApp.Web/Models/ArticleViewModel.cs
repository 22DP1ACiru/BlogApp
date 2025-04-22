namespace BlogApp.Web.Models
{
    public class ArticleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime PublishedDate { get; set; }
        public string AuthorName { get; set; }
        public string? AuthorProfilePictureUrl { get; set; }
        public bool IsPublished { get; set; }

        // Flag to indicate if the current logged-in user can edit/delete this article
        public bool CanModify { get; set; } = false;
    }
}