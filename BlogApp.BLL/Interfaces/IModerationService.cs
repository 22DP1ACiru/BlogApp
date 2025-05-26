using BlogApp.BLL.DTOs;

namespace BlogApp.BLL.Interfaces
{
    /// <summary>
    /// Defines operations for content moderation, primarily focused on comments.
    /// </summary>
    public interface IModerationService
    {
        /// <summary>
        /// Allows a user to report a comment.
        /// </summary>
        /// <param name="commentId">The ID of the comment being reported.</param>
        /// <param name="reporterUserId">The ID of the user submitting the report.</param>
        /// <param name="reason">An optional reason for the report.</param>
        /// <returns>True if the report was successfully submitted; otherwise, false.</returns>
        Task<bool> ReportCommentAsync(int commentId, string reporterUserId, string? reason);

        /// <summary>
        /// Retrieves a list of comments that have pending reports, grouped by comment,
        /// including details of individual reports.
        /// </summary>
        Task<IEnumerable<GroupedCommentReportDto>> GetGroupedPendingReportsAsync();

        /// <summary>
        /// Blocks a comment, preventing it from being publicly visible, and actions associated pending reports.
        /// </summary>
        /// <param name="commentId">The ID of the comment to block.</param>
        /// <param name="adminUserId">The ID of the administrator performing the action.</param>
        /// <returns>True if the comment was successfully blocked; otherwise, false.</returns>
        Task<bool> BlockCommentAsync(int commentId, string adminUserId);

        /// <summary>
        /// Unblocks a previously blocked comment, making it publicly visible again.
        /// </summary>
        /// <param name="commentId">The ID of the comment to unblock.</param>
        /// <param name="adminUserId">The ID of the administrator performing the action.</param>
        /// <returns>True if the comment was successfully unblocked; otherwise, false.</returns>
        Task<bool> UnblockCommentAsync(int commentId, string adminUserId);

        /// <summary>
        /// Dismisses a specific report on a comment without blocking the comment itself.
        /// The report status is updated (e.g., to 'Reviewed').
        /// </summary>
        /// <param name="reportId">The ID of the report to dismiss.</param>
        /// <param name="adminUserId">The ID of the administrator performing the action.</param>
        /// <returns>True if the report was successfully dismissed; otherwise, false.</returns>
        Task<bool> DismissReportAsync(int reportId, string adminUserId);
    }
}