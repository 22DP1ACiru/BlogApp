using System;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Defines the Unit of Work pattern, providing access to repositories
    /// and a method to commit all changes to the data store.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets the repository for Article entities.
        /// </summary>
        IArticleRepository Articles { get; }

        /// <summary>
        /// Gets the repository for ArticleVote entities.
        /// </summary>
        IArticleVoteRepository ArticleVotes { get; }

        /// <summary>
        /// Gets the repository for Comment entities.
        /// </summary>
        ICommentRepository Comments { get; }

        /// <summary>
        /// Gets the repository for CommentReport entities.
        /// </summary>
        ICommentReportRepository CommentReports { get; }

        /// <summary>
        /// Asynchronously saves all changes made in this unit of work to the underlying database.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        Task<int> CompleteAsync();
    }
}