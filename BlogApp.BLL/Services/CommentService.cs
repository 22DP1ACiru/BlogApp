using BlogApp.BLL.Interfaces;
using BlogApp.BLL.Helpers;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.DAL.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BlogApp.BLL.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<CommentService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> CanUserCommentAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("CanUserCommentAsync called with null or empty userId.");
                return false;
            }
            // Define roles allowed to comment
            var allowedRoles = new[] { AppRoles.Commenter, AppRoles.Administrator };
            return await UserRoleHelper.IsUserInAnyRoleAsync(_userManager, userId, allowedRoles);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId)
        {
            try
            {
                var comments = await _unitOfWork.Comments.GetCommentsByArticleIdAsync(articleId);

                // Filter out blocked comments for general public view
                return comments.Where(c => !c.IsBlocked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving non-blocked comments for Article ID {ArticleId}.", articleId);
                return Enumerable.Empty<Comment>();
            }
        }

        public async Task<IEnumerable<Comment>> GetAllCommentsForArticleIncludingBlockedAsync(int articleId)
        {
            try
            {
                return await _unitOfWork.Comments.GetCommentsByArticleIdAsync(articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all comments (including blocked) for Article ID {ArticleId}.", articleId);
                return Enumerable.Empty<Comment>();
            }
        }

        public async Task<Comment?> GetCommentByIdAsync(int commentId)
        {
            try
            {
                return await _unitOfWork.Comments.GetByIdAsync(commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comment with ID {CommentId}.", commentId);
                return null;
            }
        }

        public async Task<Comment?> AddCommentAsync(Comment comment, int articleId, string userId)
        {
            if (comment == null)
            {
                _logger.LogWarning("AddCommentAsync called with a null comment object for Article ID {ArticleId} by User ID {UserId}.", articleId, userId);
                return null;
            }
            if (articleId <= 0)
            {
                _logger.LogWarning("AddCommentAsync called with invalid Article ID {ArticleId} by User ID {UserId}.", articleId, userId);
                return null;
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("AddCommentAsync called with null or empty User ID for Article ID {ArticleId}.", articleId);
                return null;
            }

            try
            {
                var articleExists = await _unitOfWork.Articles.GetByIdAsync(articleId) != null;
                if (!articleExists)
                {
                    _logger.LogWarning("AddCommentAsync failed: Article ID {ArticleId} not found for comment by User ID {UserId}.", articleId, userId);
                    return null;
                }

                comment.ArticleId = articleId;
                comment.UserId = userId;
                comment.CreatedDate = DateTime.UtcNow;
                comment.LastUpdatedDate = null;
                comment.IsBlocked = false;

                await _unitOfWork.Comments.AddAsync(comment);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Comment ID {CommentId} added to Article ID {ArticleId} by User ID {UserId}.", comment.Id, articleId, userId);
                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to Article ID {ArticleId} by User ID {UserId}. Content: '{CommentContent}'.", articleId, userId, comment.Content);
                return null;
            }
        }

        public async Task<bool> UpdateCommentAsync(Comment commentToUpdate)
        {
            if (commentToUpdate == null)
            {
                _logger.LogWarning("UpdateCommentAsync called with a null comment object.");
                return false;
            }

            try
            {
                var existingComment = await _unitOfWork.Comments.GetByIdAsync(commentToUpdate.Id);
                if (existingComment == null)
                {
                    _logger.LogWarning("UpdateCommentAsync failed: Comment ID {CommentId} not found.", commentToUpdate.Id);
                    return false;
                }

                existingComment.Content = commentToUpdate.Content;
                existingComment.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment ID {CommentId} updated successfully.", commentToUpdate.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment ID {CommentId}. New Content: '{CommentContent}'.", commentToUpdate.Id, commentToUpdate.Content);
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("DeleteCommentAsync failed: Comment ID {CommentId} not found.", commentId);
                    return false;
                }

                _unitOfWork.Comments.Remove(comment);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Comment ID {CommentId} deleted successfully.", commentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment ID {CommentId}.", commentId);
                return false;
            }
        }

        public async Task<bool> CanUserEditCommentAsync(int commentId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("CanUserEditCommentAsync called with null or empty userId for comment {CommentId}.", commentId);
                return false;
            }

            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("Edit check failed for User {UserId}: Comment {CommentId} not found.", userId, commentId);
                    return false;
                }

                bool isAuthor = comment.UserId == userId;
                if (!isAuthor)
                {
                    _logger.LogInformation("Edit permission denied for user {UserId} on comment {CommentId}. User is not the author.", userId, commentId);
                }
                return isAuthor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking edit permission for Comment ID {CommentId} by User ID {UserId}.", commentId, userId);
                return false;
            }
        }

        public async Task<bool> CanUserDeleteCommentAsync(int commentId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("CanUserDeleteCommentAsync called with null or empty userId for comment {CommentId}.", commentId);
                return false;
            }

            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("Delete check failed for User {UserId}: Comment {CommentId} not found.", userId, commentId);
                    return false;
                }

                // Author can delete their own comment
                if (comment.UserId == userId)
                {
                    return true;
                }

                // Administrator can delete any comment
                if (await UserRoleHelper.IsUserInRoleAsync(_userManager, userId, AppRoles.Administrator))
                {
                    return true;
                }

                _logger.LogInformation("Delete permission denied for user {UserId} on comment {CommentId}. User is not author or administrator.", userId, commentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delete permission for Comment ID {CommentId} by User ID {UserId}.", commentId, userId);
                return false;
            }
        }
    }
}