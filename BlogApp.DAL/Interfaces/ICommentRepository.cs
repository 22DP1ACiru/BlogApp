using BlogApp.Core.Entities;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Extends the generic repository with methods specific to Comment entities.
    /// </summary>
    public interface ICommentRepository : IRepository<Comment>
    {
        /// <summary>
        /// Retrieves all comments for a specific article, including the commenter's user information.
        /// Ordered by creation date ascending.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <remarks>
        /// The service layer is responsible for filtering out blocked comments if needed for public views.
        /// </remarks>
        Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId);

        /// <summary>
        /// Retrieves a single comment by its ID, including the commenter's user information.
        /// </summary>
        /// <param name="commentId">The ID of the comment.</param>
        Task<Comment?> GetCommentByIdWithUserAsync(int commentId);
    }
}