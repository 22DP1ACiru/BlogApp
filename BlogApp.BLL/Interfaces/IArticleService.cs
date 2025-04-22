using BlogApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp.BLL.Interfaces
{
    public interface IArticleService
    {
        /// <summary>
        /// Gets all published articles.
        /// Used for public listing.
        /// </summary>
        Task<IEnumerable<Article>> GetAllPublishedArticlesWithAuthorsAsync();

        /// <summary>
        /// Gets a single article by its ID, including author information.
        /// </summary>
        Task<Article?> GetArticleByIdWithAuthorAsync(int id);

        /// <summary>
        /// Gets all articles written by a specific user.
        /// </summary>
        Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId);

        /// <summary>
        /// Creates a new article. Handles setting AuthorId and PublishedDate.
        /// </summary>
        /// <param name="article">The article entity to create (AuthorId will be overwritten).</param>
        /// <param name="authorId">The ID of the user creating the article.</param>
        /// <returns>The created article entity, or null if creation failed.</returns>
        Task<Article?> CreateArticleAsync(Article article, string authorId);

        /// <summary>
        /// Updates an existing article. Ensures timestamps or other fields are handled correctly.
        /// </summary>
        /// <param name="articleToUpdate">The article entity with updated values.</param>
        /// <returns>True if update was successful, false otherwise.</returns>
        Task<bool> UpdateArticleAsync(Article articleToUpdate);

        /// <summary>
        /// Deletes an article by its ID.
        /// </summary>
        /// <param name="id">The ID of the article to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteArticleAsync(int id);

        /// <summary>
        /// Checks if a user is authorized to modify (edit/delete) a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <param name="userId">The ID of the user attempting the action.</param>
        /// <returns>True if the user is authorized, false otherwise.</returns>
        Task<bool> CanUserModifyArticleAsync(int articleId, string userId);
    }
}