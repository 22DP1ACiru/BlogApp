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
            return await _context.Articles.Where(a => a.IsPublished).ToListAsync();
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
            var topArticleIds = await _context.ArticleVotes
                .GroupBy(v => v.ArticleId)
                .Select(g => new { ArticleId = g.Key, Score = g.Sum(v => v.VoteValue) })
                .OrderByDescending(s => s.Score)
                .Take(count)
                .Select(s => s.ArticleId)
                .ToListAsync();

            var topArticles = await _context.Articles
                .Where(a => a.IsPublished && topArticleIds.Contains(a.Id))
                .Include(a => a.Author)
                .ToListAsync();

            return topArticles.OrderBy(a => topArticleIds.IndexOf(a.Id)).ToList();
        }

        public async Task<IEnumerable<Article>> GetLastCommentedArticlesAsync(int count)
        {
            var lastCommentedArticleIds = await _context.Comments
                .Where(c => c.Article.IsPublished)
                .GroupBy(c => c.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    LastCommentDate = g.Max(c => c.CreatedDate)
                })
                .OrderByDescending(x => x.LastCommentDate)
                .Take(count)
                .Select(x => x.ArticleId)
                .ToListAsync();

            var lastCommentedArticles = await _context.Articles
               .Where(a => lastCommentedArticleIds.Contains(a.Id))
               .Include(a => a.Author)
               .ToListAsync();

            return lastCommentedArticles.OrderBy(a => lastCommentedArticleIds.IndexOf(a.Id)).ToList();
        }

        public async Task<IEnumerable<Article>> SearchPublishedArticlesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Enumerable.Empty<Article>();
            }

            var lowerSearchTerm = searchTerm.ToLowerInvariant();

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