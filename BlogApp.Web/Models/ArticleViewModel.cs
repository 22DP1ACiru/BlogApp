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

        public bool CanModify { get; set; } = false;
        public int Score { get; set; } = 0; 
        public int? CurrentUserVote { get; set; } = null;

        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    }
}