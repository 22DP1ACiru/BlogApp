using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogApp.BLL.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ArticleService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ArticleService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<ArticleService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IEnumerable<Article>> GetAllPublishedArticlesWithAuthorsAsync()
        {
            try
            {
                var articles = await _unitOfWork.Articles.FindAsync(a => a.IsPublished);
                return articles.OrderByDescending(a => a.PublishedDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all published articles with authors.");
                return Enumerable.Empty<Article>();
            }
        }

        public async Task<Article?> GetArticleByIdWithAuthorAsync(int id)
        {
            try
            {
                return await _unitOfWork.Articles.GetArticleWithAuthorAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting article with ID {ArticleId}.", id);
                return null;
            }
        }

        public async Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId)
        {
            try
            {
                return await _unitOfWork.Articles.GetArticlesByAuthorIdAsync(authorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting articles for author ID {AuthorId}.", authorId);
                return Enumerable.Empty<Article>();
            }
        }

        public async Task<Article?> CreateArticleAsync(Article article, string authorId)
        {
            if (article == null || string.IsNullOrWhiteSpace(authorId))
            {
                _logger.LogWarning("CreateArticleAsync call failed: Article object was null or authorId was empty.");
                return null;
            }

            try
            {
                article.AuthorId = authorId;
                article.PublishedDate = DateTime.UtcNow;
                article.LastUpdatedDate = article.PublishedDate;

                await _unitOfWork.Articles.AddAsync(article);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Article created successfully with ID {ArticleId} by Author {AuthorId}", article.Id, authorId);
                return article;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating article by author {AuthorId}. Title: {ArticleTitle}", authorId, article.Title);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error creating article by author {AuthorId}. Title: {ArticleTitle}", authorId, article.Title);
                return null;
            }
        }

        public async Task<bool> UpdateArticleAsync(Article articleToUpdate)
        {
            if (articleToUpdate == null)
            {
                _logger.LogWarning("UpdateArticleAsync call failed: articleToUpdate object was null.");
                return false;
            }

            try
            {
                var existingArticle = await _unitOfWork.Articles.GetByIdAsync(articleToUpdate.Id);
                if (existingArticle == null)
                {
                    _logger.LogWarning("Update failed: Article with ID {ArticleId} not found.", articleToUpdate.Id);
                    return false;
                }

                existingArticle.Title = articleToUpdate.Title;
                existingArticle.Content = articleToUpdate.Content;
                existingArticle.ImageUrl = articleToUpdate.ImageUrl;
                existingArticle.IsPublished = articleToUpdate.IsPublished;
                existingArticle.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Article updated successfully with ID {ArticleId}", articleToUpdate.Id);
                return true;
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                _logger.LogError(concurrencyEx, "Concurrency error updating article with ID {ArticleId}. Someone else may have modified it.", articleToUpdate.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating article with ID {ArticleId}.", articleToUpdate.Id);
                return false;
            }
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            Article? article = null;
            string? imageUrlToDelete = null;
            try
            {
                article = await _unitOfWork.Articles.GetByIdAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("Delete failed: Article with ID {ArticleId} not found.", id);
                    return false;
                }

                imageUrlToDelete = article.ImageUrl;

                _unitOfWork.Articles.Remove(article);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Article record deleted successfully from DB with ID {ArticleId}", id);

                // Delete associated image file after removing entity from DB
                if (!string.IsNullOrEmpty(imageUrlToDelete))
                {
                    DeleteArticleImageFile(imageUrlToDelete, article.Id); // Call helper method
                }

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting article with ID {ArticleId}.", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error deleting article with ID {ArticleId}.", id);
                return false;
            }
        }

        private void DeleteArticleImageFile(string imageUrl, int articleId)
        {
            try
            {
                if (!imageUrl.StartsWith("/images/profiles/"))
                {
                    _logger.LogWarning("Skipping file deletion for article {ArticleId}. ImageUrl '{ImageUrl}' does not match expected format.", articleId, imageUrl);
                    return;
                }

                var relativePath = imageUrl.TrimStart('/');
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath);
                string filePath = Path.Combine(uploadsFolder, relativePath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Deleted article image file successfully: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Article image file not found for deletion: {FilePath}", filePath);
                }
            }
            catch (IOException ioEx)
            {
                // Log IO errors specifically (permissions, file in use, etc.)
                _logger.LogError(ioEx, "IO Error deleting article image file {FilePath} for article {ArticleId}.", imageUrl, articleId);
            }
            catch (Exception ex)
            {
                // Log other potential errors (path issues etc.)
                _logger.LogError(ex, "Error deleting article image file for URL {ImageUrl}, Article ID {ArticleId}.", imageUrl, articleId);
            }
        }


        public async Task<bool> CanUserModifyArticleAsync(int articleId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            try
            {
                var article = await _unitOfWork.Articles.GetByIdAsync(articleId);
                if (article == null)
                {
                    _logger.LogWarning("Authorization check failed: Article {ArticleId} not found.", articleId);
                    return false;
                }

                if (article.AuthorId == userId) return true;

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.Administrator)) return true;

                _logger.LogWarning("Authorization denied for user {UserId} on article {ArticleId}. User is not author or admin.", userId, articleId);
                return false;
            }
            catch (Exception ex)
            {
                // Log reason for failure
                _logger.LogError(ex, "Error during authorization check for user {UserId} on article {ArticleId}.", userId, articleId);
                return false;
            }
        }

        public async Task<IEnumerable<Article>> GetAllArticlesForManagementAsync()
        {
            try
            {
                return await _unitOfWork.Articles.GetArticlesWithAuthorsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all articles for management.");
                return Enumerable.Empty<Article>();
            }
        }
    }
}