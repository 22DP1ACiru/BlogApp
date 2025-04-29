using BlogApp.Core.Entities;

namespace BlogApp.DAL.Interfaces
{
    public interface IArticleVoteRepository
    {
        Task AddAsync(ArticleVote vote);
        void Update(ArticleVote vote);
        void Remove(ArticleVote vote);
        Task<ArticleVote?> FindByUserAndArticleAsync(string userId, int articleId);
        Task<IEnumerable<ArticleVote>> GetVotesForArticleAsync(int articleId);
        Task<int> GetScoreForArticleAsync(int articleId);
    }
}