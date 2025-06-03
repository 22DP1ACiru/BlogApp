using BlogApp.Core.Entities;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Defines repository operations specific to ArticleVote entities.
    /// </summary>
    public interface IArticleVoteRepository
    {
        /// <summary>
        /// Adds a new article vote to the data store.
        /// </summary>
        /// <param name="vote">The ArticleVote entity to add.</param>
        Task AddAsync(ArticleVote vote);

        /// <summary>
        /// Marks an existing article vote as modified in the data store.
        /// </summary>
        /// <param name="vote">The ArticleVote entity to update.</param>
        void Update(ArticleVote vote);

        /// <summary>
        /// Removes an article vote from the data store.
        /// </summary>
        /// <param name="vote">The ArticleVote entity to remove.</param>
        void Remove(ArticleVote vote);

        /// <summary>
        /// Finds a specific vote cast by a user on a particular article.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="articleId">The ID of the article.</param>
        /// <returns>The ArticleVote if found; otherwise, null.</returns>
        Task<ArticleVote?> FindByUserAndArticleAsync(string userId, int articleId);

        /// <summary>
        /// Retrieves all votes associated with a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        Task<IEnumerable<ArticleVote>> GetVotesForArticleAsync(int articleId);

        /// <summary>
        /// Calculates the aggregate score (sum of VoteValue) for a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <returns>The total score of the article.</returns>
        Task<int> GetScoreForArticleAsync(int articleId);
    }
}