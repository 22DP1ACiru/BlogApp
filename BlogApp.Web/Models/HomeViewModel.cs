namespace BlogApp.Web.Models
{
    public class HomeViewModel
    {
        public string? SearchTerm { get; set; }
        public List<ArticleViewModel> LatestArticles { get; set; } = new List<ArticleViewModel>();
        public List<ArticleViewModel> TopRankedArticles { get; set; } = new List<ArticleViewModel>();
        public List<ArticleViewModel> LastCommentedArticles { get; set; } = new List<ArticleViewModel>();
        public List<ArticleViewModel>? SearchResults { get; set; } = null;
    }
}