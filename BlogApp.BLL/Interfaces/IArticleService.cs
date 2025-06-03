using BlogApp.Core.Entities;

namespace BlogApp.BLL.Interfaces
{
    /// <summary>
    /// Defines operations for managing articles.
    /// </summary>
    public interface IArticleService
    {
        /// <summary>
        /// Retrieves all published articles, including their author information.
        /// </summary>
        Task<IEnumerable<Article>> GetAllPublishedArticlesWithAuthorsAsync();

        /// <summary>
        /// Retrieves a single published article by its ID, including author information.
        /// </summary>
        Task<Article?> GetArticleByIdWithAuthorAsync(int id);

        /// <summary>
        /// Retrieves all articles written by a specific author.
        /// </summary>
        Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId);

        /// <summary>
        /// Creates a new article.
        /// </summary>
        /// <param name="article">The article entity to create.</param>
        /// <param name="authorId">The ID of the user creating the article.</param>
        /// <returns>The created article entity, or null if creation failed.</returns>
        Task<Article?> CreateArticleAsync(Article article, string authorId);

        /// <summary>
        /// Updates an existing article.
        /// </summary>
        /// <param name="articleToUpdate">The article entity with updated values.</param>
        /// <returns>True if update was successful; otherwise, false.</returns>
        Task<bool> UpdateArticleAsync(Article articleToUpdate);

        /// <summary>
        /// Deletes an article by its ID.
        /// </summary>
        /// <param name="id">The ID of the article to delete.</param>
        /// <returns>True if deletion was successful; otherwise, false.</returns>
        Task<bool> DeleteArticleAsync(int id);

        /// <summary>
        /// Determines if a user is authorized to modify (edit/delete) a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <param name="userId">The ID of the user attempting the action.</param>
        /// <returns>True if the user is authorized; otherwise, false.</returns>
        Task<bool> CanUserModifyArticleAsync(int articleId, string userId);

        /// <summary>
        /// Retrieves the latest published articles up to a specified count.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetLatestPublishedArticlesAsync(int count);

        /// <summary>
        /// Retrieves the top-ranked published articles up to a specified count.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetTopRankedArticlesAsync(int count);

        /// <summary>
        /// Retrieves the most recently commented-on published articles up to a specified count.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetLastCommentedArticlesAsync(int count);

        /// <summary>
        /// Searches published articles by a given search term in title or content.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        Task<IEnumerable<Article>> SearchPublishedArticlesAsync(string searchTerm);
    }
}