using BlogApp.Core.Entities;

namespace BlogApp.BLL.Interfaces
{
    /// <summary>
    /// Defines operations for managing comments.
    /// </summary>
    public interface ICommentService
    {
        /// <summary>
        /// Retrieves all non-blocked comments for a specific article.
        /// </summary>
        Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId);

        /// <summary>
        /// Retrieves all comments for a specific article, including blocked ones (typically for admin use).
        /// </summary>
        Task<IEnumerable<Comment>> GetAllCommentsForArticleIncludingBlockedAsync(int articleId);

        /// <summary>
        /// Retrieves a specific comment by its ID.
        /// </summary>
        Task<Comment?> GetCommentByIdAsync(int commentId);

        /// <summary>
        /// Adds a new comment to an article.
        /// </summary>
        /// <param name="comment">The comment entity to add.</param>
        /// <param name="articleId">The ID of the article to which the comment belongs.</param>
        /// <param name="userId">The ID of the user posting the comment.</param>
        /// <returns>The added comment entity, or null if addition failed.</returns>
        Task<Comment?> AddCommentAsync(Comment comment, int articleId, string userId);

        /// <summary>
        /// Updates an existing comment.
        /// </summary>
        /// <param name="commentToUpdate">The comment entity with updated values.</param>
        /// <returns>True if update was successful; otherwise, false.</returns>
        Task<bool> UpdateCommentAsync(Comment commentToUpdate);

        /// <summary>
        /// Deletes a comment by its ID.
        /// </summary>
        /// <param name="commentId">The ID of the comment to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        Task<bool> DeleteCommentAsync(int commentId);

        /// <summary>
        /// Determines if a user is authorized to edit a specific comment.
        /// </summary>
        Task<bool> CanUserEditCommentAsync(int commentId, string userId);

        /// <summary>
        /// Determines if a user is authorized to delete a specific comment.
        /// </summary>
        Task<bool> CanUserDeleteCommentAsync(int commentId, string userId);

        /// <summary>
        /// Determines if a user has the necessary role(s) to post comments.
        /// </summary>
        Task<bool> CanUserCommentAsync(string userId);
    }
}