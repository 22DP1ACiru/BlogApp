using BlogApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId);
        Task<Comment?> GetCommentByIdWithUserAsync(int commentId);
    }
}