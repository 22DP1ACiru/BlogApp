using BlogApp.Core.Entities;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Extends the generic repository with methods specific to CommentReport entities.
    /// </summary>
    public interface ICommentReportRepository : IRepository<CommentReport>
    {
        /// <summary>
        /// Retrieves all comment reports that are currently in 'Pending' status,
        /// including details of the associated comment, article, and reporter user.
        /// Ordered by report date descending.
        /// </summary>
        Task<IEnumerable<CommentReport>> GetPendingReportsWithDetailsAsync();

        /// <summary>
        /// Retrieves a single comment report by its ID, including details of the
        /// associated comment, article, reporter user, and reviewing admin (if any).
        /// </summary>
        /// <param name="reportId">The ID of the report.</param>
        Task<CommentReport?> GetReportByIdWithDetailsAsync(int reportId);

        /// <summary>
        /// Checks if a specific user already has a 'Pending' report for a specific comment.
        /// </summary>
        /// <param name="commentId">The ID of the comment.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>True if a pending report exists from this user for this comment; otherwise, false.</returns>
        Task<bool> HasUserPendingReportForCommentAsync(int commentId, string userId);
    }
}