using System.Diagnostics;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using BlogApp.BLL.Interfaces;
using BlogApp.Core.Entities;

namespace BlogApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IArticleService _articleService;
        private readonly IRankingService _rankingService;

        public HomeController(ILogger<HomeController> logger, IArticleService articleService, IRankingService rankingService)
        {
            _logger = logger;
            _articleService = articleService;
            _rankingService = rankingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchString = null)
        {
            var homeViewModel = new HomeViewModel
            {
                SearchTerm = searchString
            };

            _logger.LogInformation("Home Index accessed. SearchTerm: '{SearchTerm}'", searchString);

            try
            {
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    _logger.LogInformation("Performing article search for: '{SearchTerm}'", searchString);
                    var searchResults = await _articleService.SearchPublishedArticlesAsync(searchString);
                    homeViewModel.SearchResults = MapToArticleViewModelList(searchResults);
                    _logger.LogInformation("Search found {ResultCount} articles.", homeViewModel.SearchResults?.Count ?? 0);
                }
                else
                {
                    _logger.LogInformation("Fetching default home page sections.");
                    var latestArticles = await _articleService.GetLatestPublishedArticlesAsync(5);
                    var topRankedArticles = await _articleService.GetTopRankedArticlesAsync(3);
                    var lastCommentedArticles = await _articleService.GetLastCommentedArticlesAsync(3);

                    homeViewModel.LatestArticles = MapToArticleViewModelList(latestArticles);
                    homeViewModel.TopRankedArticles = MapToArticleViewModelList(topRankedArticles);
                    homeViewModel.LastCommentedArticles = MapToArticleViewModelList(lastCommentedArticles);

                    if (homeViewModel.TopRankedArticles.Any())
                    {
                        _logger.LogInformation("Fetching scores for {Count} top ranked articles.", homeViewModel.TopRankedArticles.Count);
                        foreach (var articleVM in homeViewModel.TopRankedArticles)
                        {
                            articleVM.Score = await _rankingService.GetArticleScoreAsync(articleVM.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for Home Index page.");
            }

            return View(homeViewModel);
        }

        private List<ArticleViewModel> MapToArticleViewModelList(IEnumerable<Article> articles)
        {
            if (articles == null || !articles.Any()) return new List<ArticleViewModel>();

            return articles.Select(a => new ArticleViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content?.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content, 
                ImageUrl = a.ImageUrl,
                PublishedDate = a.PublishedDate,
                AuthorName = a.Author?.UserName ?? "Unknown",
                AuthorProfilePictureUrl = a.Author?.ProfilePictureUrl,
            }).ToList();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
