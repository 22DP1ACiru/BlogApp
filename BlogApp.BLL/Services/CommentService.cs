using BlogApp.BLL.Interfaces;
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

        public CommentService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<CommentService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> CanUserCommentAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && (await _userManager.IsInRoleAsync(user, AppRoles.Commenter) || await _userManager.IsInRoleAsync(user, AppRoles.Administrator));
        }

        public async Task<IEnumerable<Comment>> GetCommentsByArticleIdAsync(int articleId)
        {
            try
            {
                return await _unitOfWork.Comments.GetCommentsByArticleIdAsync(articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for Article {ArticleId}", articleId);
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
                _logger.LogError(ex, "Error getting comment with ID {CommentId}.", commentId);
                return null;
            }
        }

        public async Task<Comment?> AddCommentAsync(Comment comment, int articleId, string userId)
        {
            if (comment == null || articleId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("AddCommentAsync failed: Invalid input parameters.");
                return null;
            }

            var articleExists = await _unitOfWork.Articles.GetByIdAsync(articleId) != null;
            if (!articleExists)
            {
                _logger.LogWarning("AddCommentAsync failed: Article {ArticleId} not found.", articleId);
                return null;
            }

            try
            {
                comment.ArticleId = articleId;
                comment.UserId = userId;
                comment.CreatedDate = DateTime.UtcNow;
                comment.LastUpdatedDate = null;

                await _unitOfWork.Comments.AddAsync(comment);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Comment {CommentId} added to Article {ArticleId} by User {UserId}", comment.Id, articleId, userId);
                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to Article {ArticleId} by User {UserId}", articleId, userId);
                return null;
            }
        }

        public async Task<bool> UpdateCommentAsync(Comment commentToUpdate)
        {
            if (commentToUpdate == null) { /* Log */ return false; }

            try
            {
                var existingComment = await _unitOfWork.Comments.GetByIdAsync(commentToUpdate.Id);
                if (existingComment == null) { return false; }

                existingComment.Content = commentToUpdate.Content;
                existingComment.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} updated successfully.", commentToUpdate.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentToUpdate.Id);
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null) { return false; }

                _unitOfWork.Comments.Remove(comment);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Comment {CommentId} deleted successfully.", commentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<bool> CanUserEditCommentAsync(int commentId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("Edit check failed: Comment {CommentId} not found.", commentId);
                    return false;
                }

                bool isAuthor = comment.UserId == userId;

                if (!isAuthor)
                {
                    _logger.LogWarning("Edit permission denied for user {UserId} on comment {CommentId}. User is not the author.", userId, commentId);
                }

                return isAuthor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking edit permission for Comment {CommentId} by User {UserId}", commentId, userId);
                return false;
            }
        }

        public async Task<bool> CanUserDeleteCommentAsync(int commentId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("Delete check failed: Comment {CommentId} not found.", commentId);
                    return false;
                }

                if (comment.UserId == userId) return true;

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.Administrator)) return true;

                _logger.LogWarning("Delete permission denied for user {UserId} on comment {CommentId}. User is not author or admin.", userId, commentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delete permission for Comment {CommentId} by User {UserId}", commentId, userId);
                return false;
            }
        }
    }
}