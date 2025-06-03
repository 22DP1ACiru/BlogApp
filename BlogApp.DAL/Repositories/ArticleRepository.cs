using BlogApp.Core.Entities;
using BlogApp.DAL.Data;
using BlogApp.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.DAL.Repositories
{
    public class ArticleRepository : Repository<Article>, IArticleRepository
    {
        public ArticleRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Article>> GetAllPublishedArticlesAsync()
        {
            return await _context.Articles
                                 .Where(a => a.IsPublished)
                                 .OrderByDescending(a => a.PublishedDate)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Article>> GetArticlesWithAuthorsAsync()
        {
            return await _context.Articles
                                 .Include(a => a.Author)
                                 .OrderByDescending(a => a.PublishedDate)
                                 .ToListAsync();
        }

        public async Task<Article?> GetArticleWithAuthorAsync(int id)
        {
            return await _context.Articles
                                 .Include(a => a.Author)
                                 .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId)
        {
            return await _context.Articles
                                .Include(a => a.Author)
                                .Where(a => a.AuthorId == authorId)
                                .OrderByDescending(a => a.PublishedDate)
                                .ToListAsync();
        }

        public async Task<IEnumerable<Article>> GetLatestPublishedArticlesAsync(int count)
        {
            return await _context.Articles
                                 .Where(a => a.IsPublished)
                                 .OrderByDescending(a => a.PublishedDate)
                                 .Take(count)
                                 .Include(a => a.Author)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Article>> GetTopRankedArticlesAsync(int count)
        {
            // Get the IDs of the top 'count' articles based on their vote sum.
            var topArticleIds = await _context.ArticleVotes
                .Where(v => v.Article.IsPublished)
                .GroupBy(v => v.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    Score = g.Sum(v => v.VoteValue)
                })
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => _context.Articles.First(a => a.Id == s.ArticleId).PublishedDate) // Tie-breaker: newer articles first
                .Take(count)
                .Select(s => s.ArticleId)
                .ToListAsync();

            if (!topArticleIds.Any())
            {
                return Enumerable.Empty<Article>();
            }

            // Fetch the actual article entities for these IDs, including authors.
            var topArticles = await _context.Articles
                .Where(a => a.IsPublished && topArticleIds.Contains(a.Id))
                .Include(a => a.Author)
                .ToListAsync();

            // Order the fetched articles according to the rank determined by topArticleIds.
            return topArticles.OrderBy(a => topArticleIds.IndexOf(a.Id));
        }

        public async Task<IEnumerable<Article>> GetLastCommentedArticlesAsync(int count)
        {
            // Get IDs of articles, ordered by their most recent comment's creation date.
            var lastCommentedArticleIds = await _context.Comments
                .Where(c => c.Article.IsPublished) // Consider only comments on published articles
                .GroupBy(c => c.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    LastCommentDate = g.Max(c => c.CreatedDate) // Get the most recent comment date for each article
                })
                .OrderByDescending(x => x.LastCommentDate)
                .Take(count)
                .Select(x => x.ArticleId)
                .ToListAsync();

            if (!lastCommentedArticleIds.Any())
            {
                return Enumerable.Empty<Article>();
            }

            // Fetch the article entities for these IDs.
            var lastCommentedArticles = await _context.Articles
               .Where(a => a.IsPublished && lastCommentedArticleIds.Contains(a.Id)) // Ensure they are published
               .Include(a => a.Author)
               .ToListAsync();

            // Order the results based on the order derived from last comment date.
            return lastCommentedArticles.OrderBy(a => lastCommentedArticleIds.IndexOf(a.Id));
        }

        public async Task<IEnumerable<Article>> SearchPublishedArticlesAsync(string searchTerm)
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant(); // For case-insensitive search

            return await _context.Articles
                .Where(a => a.IsPublished &&
                            (a.Title.ToLower().Contains(lowerSearchTerm) ||
                             (a.Content != null && a.Content.ToLower().Contains(lowerSearchTerm))))
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishedDate)
                .ToListAsync();
        }
    }
}