namespace BlogApp.BLL.Interfaces
{
    /// <summary>
    /// Defines operations related to article ranking and voting.
    /// </summary>
    public interface IRankingService
    {
        /// <summary>
        /// Records a user's vote on an article. If the user has already voted,
        /// their vote may be updated or removed (toggled).
        /// </summary>
        /// <param name="articleId">The ID of the article being voted on.</param>
        /// <param name="userId">The ID of the user casting the vote.</param>
        /// <param name="voteValue">The value of the vote (e.g., +1 for upvote, -1 for downvote).</param>
        /// <returns>True if the vote was successfully processed; otherwise, false.</returns>
        Task<bool> VoteAsync(int articleId, string userId, int voteValue);

        /// <summary>
        /// Gets the current aggregate score (sum of all votes) for a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <returns>The calculated score for the article.</returns>
        Task<int> GetArticleScoreAsync(int articleId);

        /// <summary>
        /// Retrieves the vote value cast by a specific user for a specific article.
        /// </summary>
        /// <param name="articleId">The ID of the article.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The vote value (+1 or -1) if the user has voted; otherwise, null.</returns>
        Task<int?> GetUserVoteForArticleAsync(int articleId, string userId);

        /// <summary>
        /// Checks if a user has the permission (e.g., appropriate role) to vote on articles.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if the user is permitted to rank/vote; otherwise, false.</returns>
        Task<bool> CanUserRankAsync(string userId);
    }
}