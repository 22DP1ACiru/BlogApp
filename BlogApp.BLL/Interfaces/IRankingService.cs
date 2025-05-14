namespace BlogApp.BLL.Interfaces
{
    public interface IRankingService
    {
        /// <summary>
        /// Records a user's vote on an article. Creates or updates the vote.
        /// </summary>
        /// <param name="articleId">The ID of the article being voted on.</param>
        /// <param name="userId">The ID of the user casting the vote.</param>
        /// <param name="voteValue">The value of the vote (+1 or -1).</param>
        /// <returns>True if the vote was successfully processed, false otherwise.</returns>
        Task<bool> VoteAsync(int articleId, string userId, int voteValue);

        /// <summary>
        /// Gets the current score (sum of votes) for an article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <returns>The calculated score.</returns>
        Task<int> GetArticleScoreAsync(int articleId);

        /// <summary>
        /// Gets the value of the vote cast by a specific user for a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The vote value (+1 or -1) if the user voted, null otherwise.</returns>
        Task<int?> GetUserVoteForArticleAsync(int articleId, string userId);

        /// <summary>
        /// Checks if a user has the permission to vote (i.e., has the Ranker role).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user can vote, false otherwise.</returns>
        Task<bool> CanUserRankAsync(string userId);
    }
}