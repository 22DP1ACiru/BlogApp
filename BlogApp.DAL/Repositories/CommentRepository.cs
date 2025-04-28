using BlogApp.Core.Entities;
using BlogApp.DAL.Data;
using BlogApp.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.DAL.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId)
        {
            return await _context.Comments
                                 .Where(c => c.ArticleId == articleId)
                                 .Include(c => c.User)
                                 .OrderBy(c => c.CreatedDate)
                                 .ToListAsync();
        }

        public async Task<Comment?> GetCommentByIdWithUserAsync(int commentId)
        {
            return await _context.Comments
                                .Include(c => c.User)
                                .FirstOrDefaultAsync(c => c.Id == commentId);
        }
    }
}