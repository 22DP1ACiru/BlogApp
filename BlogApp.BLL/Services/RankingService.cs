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

        public RankingService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<RankingService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> CanUserRankAsync(string userId)
        {
            var allowedRoles = new[] { AppRoles.Ranker, AppRoles.Administrator };

            return await UserRoleHelper.IsUserInAnyRoleAsync(
                _userManager,
                userId,
                allowedRoles);
        }

        public async Task<bool> VoteAsync(int articleId, string userId, int voteValue)
        {
            if (voteValue != 1 && voteValue != -1)
            {
                _logger.LogWarning("Invalid vote value {VoteValue} provided for Article {ArticleId} by User {UserId}", voteValue, articleId, userId);
                return false;
            }

            // Check if article exists
            var articleExists = await _unitOfWork.Articles.GetByIdAsync(articleId) != null;
            if (!articleExists)
            {
                _logger.LogWarning("User {UserId} attempted to vote on non-existent Article {ArticleId}.", userId, articleId);
                return false;
            }


            try
            {
                var existingVote = await _unitOfWork.ArticleVotes.FindByUserAndArticleAsync(userId, articleId);

                if (existingVote != null)
                {
                    // User has voted before on this article
                    if (existingVote.VoteValue == voteValue)
                    {
                        // User clicked the same button again - remove the vote (toggle off)
                        _unitOfWork.ArticleVotes.Remove(existingVote);
                        _logger.LogInformation("Vote removed for Article {ArticleId} by User {UserId}", articleId, userId);
                    }
                    else
                    {
                        // User changed their vote (e.g., from +1 to -1)
                        existingVote.VoteValue = voteValue;
                        existingVote.VotedDate = DateTime.UtcNow;
                        _unitOfWork.ArticleVotes.Update(existingVote); // Mark as updated
                        _logger.LogInformation("Vote changed to {VoteValue} for Article {ArticleId} by User {UserId}", voteValue, articleId, userId);
                    }
                }
                else
                {
                    // New vote
                    var newVote = new ArticleVote
                    {
                        ArticleId = articleId,
                        UserId = userId,
                        VoteValue = voteValue,
                        VotedDate = DateTime.UtcNow
                    };
                    await _unitOfWork.ArticleVotes.AddAsync(newVote);
                    _logger.LogInformation("New vote {VoteValue} recorded for Article {ArticleId} by User {UserId}", voteValue, articleId, userId);
                }

                await _unitOfWork.CompleteAsync(); // Save changes
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vote for Article {ArticleId} by User {UserId}", articleId, userId);
                return false;
            }
        }

        public async Task<int> GetArticleScoreAsync(int articleId)
        {
            try
            {
                return await _unitOfWork.ArticleVotes.GetScoreForArticleAsync(articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting score for Article {ArticleId}", articleId);
                return 0;
            }
        }

        public async Task<int?> GetUserVoteForArticleAsync(int articleId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null; // No vote if no user ID

            try
            {
                var vote = await _unitOfWork.ArticleVotes.FindByUserAndArticleAsync(userId, articleId);
                return vote?.VoteValue; // Return the value or null if vote doesn't exist
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user vote for Article {ArticleId} by User {UserId}", articleId, userId);
                return null; // Return null on error
            }
        }
    }
}