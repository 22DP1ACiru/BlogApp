namespace BlogApp.Web.DTOs
{
    public class ArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime PublishedDate { get; set; }
        public string AuthorUsername { get; set; }
        public bool IsPublished { get; set; }
        public int Score { get; set; }
    }
}