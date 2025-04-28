using System;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IArticleRepository Articles { get; }
        IArticleVoteRepository ArticleVotes { get; }
        ICommentRepository Comments { get; }

        Task<int> CompleteAsync();
    }
}