using BlogApp.Core.Entities;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Extends the generic repository with methods specific to Article entities.
    /// </summary>
    public interface IArticleRepository : IRepository<Article>
    {
        /// <summary>
        /// Retrieves all articles that are marked as published.
        /// Does not typically include author details unless specified by implementation.
        /// </summary>
        Task<IEnumerable<Article>> GetAllPublishedArticlesAsync();

        /// <summary>
        /// Retrieves all articles, including their author information.
        /// Ordered by published date descending.
        /// </summary>
        Task<IEnumerable<Article>> GetArticlesWithAuthorsAsync();

        /// <summary>
        /// Retrieves a single article by its ID, including its author information.
        /// </summary>
        /// <param name="id">The ID of the article.</param>
        Task<Article?> GetArticleWithAuthorAsync(int id);

        /// <summary>
        /// Retrieves all articles written by a specific author, including author information.
        /// Ordered by published date descending.
        /// </summary>
        /// <param name="authorId">The ID of the author.</param>
        Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId);

        /// <summary>
        /// Retrieves the latest published articles up to a specified count, including author information.
        /// Ordered by published date descending.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetLatestPublishedArticlesAsync(int count);

        /// <summary>
        /// Retrieves the top-ranked published articles up to a specified count, including author information.
        /// Articles are ordered by their rank score.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetTopRankedArticlesAsync(int count);

        /// <summary>
        /// Retrieves the most recently commented-on published articles up to a specified count, including author information.
        /// Articles are ordered by the date of their latest comment.
        /// </summary>
        /// <param name="count">The maximum number of articles to retrieve.</param>
        Task<IEnumerable<Article>> GetLastCommentedArticlesAsync(int count);

        /// <summary>
        /// Searches published articles by a given search term in their title or content, including author information.
        /// Ordered by published date descending.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        Task<IEnumerable<Article>> SearchPublishedArticlesAsync(string searchTerm);
    }
}