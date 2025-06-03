using BlogApp.BLL.Helpers;
using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.DAL.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BlogApp.BLL.Services
{
    public class RankingService : IRankingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RankingService> _logger;

        public RankingService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<RankingService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> CanUserRankAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("CanUserRankAsync called with null or empty userId.");
                return false;
            }
            // Define roles allowed to rank articles
            var allowedRoles = new[] { AppRoles.Ranker, AppRoles.Administrator };
            return await UserRoleHelper.IsUserInAnyRoleAsync(_userManager, userId, allowedRoles);
        }

        public async Task<bool> VoteAsync(int articleId, string userId, int voteValue)
        {
            if (articleId <= 0)
            {
                _logger.LogWarning("VoteAsync: Invalid ArticleId {ArticleId} provided by User {UserId}.", articleId, userId);
                return false;
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("VoteAsync: Null or empty UserId provided for ArticleId {ArticleId}.", articleId);
                return false;
            }
            if (voteValue != 1 && voteValue != -1)
            {
                _logger.LogWarning("Invalid vote value {VoteValue} provided for Article {ArticleId} by User {UserId}. Vote ignored.", voteValue, articleId, userId);
                return false;
            }

            try
            {
                var article = await _unitOfWork.Articles.GetByIdAsync(articleId);
                if (article == null)
                {
                    _logger.LogWarning("User {UserId} attempted to vote on non-existent Article {ArticleId}.", userId, articleId);
                    return false;
                }

                var existingVote = await _unitOfWork.ArticleVotes.FindByUserAndArticleAsync(userId, articleId);

                if (existingVote != null)
                {
                    // User has voted before on this article
                    if (existingVote.VoteValue == voteValue)
                    {
                        // User clicked the same vote button again (e.g., upvoted then clicked upvote again), remove vote
                        _unitOfWork.ArticleVotes.Remove(existingVote);
                        _logger.LogInformation("Vote removed for Article {ArticleId} by User {UserId} (was {OriginalVoteValue}).", articleId, userId, existingVote.VoteValue);
                    }
                    else
                    {
                        // User changed their vote (e.g., from upvote to downvote, or vice-versa), update value
                        existingVote.VoteValue = voteValue;
                        existingVote.VotedDate = DateTime.UtcNow;
                        _unitOfWork.ArticleVotes.Update(existingVote);
                        _logger.LogInformation("Vote changed to {NewVoteValue} for Article {ArticleId} by User {UserId} (was {OriginalVoteValue}).", voteValue, articleId, userId, existingVote.VoteValue * -1);
                    }
                }
                else
                {
                    // New vote for this user on this article
                    var newVote = new ArticleVote
                    {
                        ArticleId = articleId,
                        UserId = userId,
                        VoteValue = voteValue,
                        VotedDate = DateTime.UtcNow
                    };
                    await _unitOfWork.ArticleVotes.AddAsync(newVote);
                    _logger.LogInformation("New vote ({VoteValue}) recorded for Article {ArticleId} by User {UserId}. Vote ID: {VoteId}", voteValue, articleId, userId, newVote.Id);
                }

                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vote for Article {ArticleId} by User {UserId} with VoteValue {VoteValue}.", articleId, userId, voteValue);
                return false;
            }
        }

        public async Task<int> GetArticleScoreAsync(int articleId)
        {
            if (articleId <= 0) return 0;
            try
            {
                return await _unitOfWork.ArticleVotes.GetScoreForArticleAsync(articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving score for Article {ArticleId}.", articleId);
                return 0;
            }
        }

        public async Task<int?> GetUserVoteForArticleAsync(int articleId, string userId)
        {
            if (articleId <= 0) return null;
            if (string.IsNullOrWhiteSpace(userId))
            {
                // Non-logged-in user or invalid user ID, cannot have a vote.
                return null;
            }

            try
            {
                var vote = await _unitOfWork.ArticleVotes.FindByUserAndArticleAsync(userId, articleId);
                return vote?.VoteValue; // Returns the vote value, or null if no vote record exists
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's ({UserId}) vote for Article {ArticleId}.", userId, articleId);
                return null;
            }
        }
    }
}