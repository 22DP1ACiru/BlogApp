using BlogApp.Core.Entities;
using BlogApp.DAL.Data;
using BlogApp.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.DAL.Repositories
{
    public class ArticleVoteRepository : IArticleVoteRepository
    {
        private readonly ApplicationDbContext _context;

        public ArticleVoteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ArticleVote vote)
        {
            await _context.ArticleVotes.AddAsync(vote);
        }

        public void Update(ArticleVote vote)
        {
            _context.Entry(vote).State = EntityState.Modified;
        }

        public void Remove(ArticleVote vote)
        {
            _context.ArticleVotes.Remove(vote);
        }


        public async Task<ArticleVote?> FindByUserAndArticleAsync(string userId, int articleId)
        {
            return await _context.ArticleVotes
                                 .FirstOrDefaultAsync(v => v.UserId == userId && v.ArticleId == articleId);
        }

        public async Task<IEnumerable<ArticleVote>> GetVotesForArticleAsync(int articleId)
        {
            return await _context.ArticleVotes
                                .Where(v => v.ArticleId == articleId)
                                .ToListAsync();
        }

        public async Task<int> GetScoreForArticleAsync(int articleId)
        {
            return await _context.ArticleVotes
                                .Where(v => v.ArticleId == articleId)
                                .SumAsync(v => v.VoteValue);
        }
    }
}