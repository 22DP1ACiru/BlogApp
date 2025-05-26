using BlogApp.Core.Entities;

namespace BlogApp.BLL.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId); // Returns non-blocked
        Task<IEnumerable<Comment>> GetAllCommentsForArticleIncludingBlockedAsync(int articleId); // Returns all for admin
        Task<Comment?> GetCommentByIdAsync(int commentId);
        Task<Comment?> AddCommentAsync(Comment comment, int articleId, string userId);
        Task<bool> UpdateCommentAsync(Comment commentToUpdate);
        Task<bool> DeleteCommentAsync(int commentId);
        Task<bool> CanUserEditCommentAsync(int commentId, string userId);
        Task<bool> CanUserDeleteCommentAsync(int commentId, string userId);
        Task<bool> CanUserCommentAsync(string userId);
    }
}