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
    }
}