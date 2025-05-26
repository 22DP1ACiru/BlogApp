using BlogApp.BLL.Interfaces;
using BlogApp.Core.Entities;
using BlogApp.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlogApp.BLL.Services
{
    public class ModerationService : IModerationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModerationService> _logger;

        public ModerationService(IUnitOfWork unitOfWork, ILogger<ModerationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> ReportCommentAsync(int commentId, string reporterUserId, string? reason)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null)
            {
                _logger.LogWarning("Failed to report: Comment {CommentId} not found. Maybe it does not exist?", commentId);
                return false;
            }

            if (comment.UserId == reporterUserId)
            {
                _logger.LogWarning("Failed to report: Comment {CommentId} belongs to Reporter {ReporterUserId}", commentId, reporterUserId);
                return false;
            }

            if (await _unitOfWork.CommentReports.HasUserPendingReportForCommentAsync(commentId, reporterUserId))
            {
                _logger.LogInformation("User {UserId} tried to report Comment {CommentId} again, but already has a pending report.", reporterUserId, commentId);
                TempDataHelper.SetWarningMessage("You already have a pending report for this comment.");
                return false;
            }

            try
            {
                var report = new CommentReport
                {
                    CommentId = commentId,
                    ReporterUserId = reporterUserId,
                    Reason = reason,
                    ReportDate = DateTime.UtcNow,
                    Status = ReportStatus.Pending // New reports are always pending
                };
                await _unitOfWork.CommentReports.AddAsync(report);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} reported by User {UserId}", commentId, reporterUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting Comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<IEnumerable<CommentReport>> GetPendingReportsAsync()
        {
            try { return await _unitOfWork.CommentReports.GetPendingReportsWithDetailsAsync(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reports, returning empty enumerable.");
                return Enumerable.Empty<CommentReport>();
            }
        }

        public async Task<bool> BlockCommentAsync(int commentId, string adminUserId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null || comment.IsBlocked) return false;

                comment.IsBlocked = true;
                comment.LastUpdatedDate = DateTime.UtcNow;

                // Find PENDING reports for this comment and mark them as Blocked
                var reports = await _unitOfWork.CommentReports.FindAsync(r => r.CommentId == commentId && r.Status == ReportStatus.Pending);
                foreach (var report in reports)
                {
                    report.Status = ReportStatus.Blocked;
                    report.ReviewedByAdminId = adminUserId;
                    report.ReviewedDate = DateTime.UtcNow;
                }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} blocked by Admin {AdminId}. Associated pending reports updated.", commentId, adminUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking Comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<bool> UnblockCommentAsync(int commentId, string adminUserId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null || !comment.IsBlocked) return false;

                comment.IsBlocked = false;
                comment.LastUpdatedDate = DateTime.UtcNow;

                // Note: We are NOT changing the status of 'Blocked' reports back to 'Pending' or 'Reviewed' here.
                // If the comment is unblocked and becomes problematic again, it should be re-reported.
                // This keeps the history of it having been blocked.

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} unblocked by Admin {AdminId}", commentId, adminUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking Comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<bool> DismissReportAsync(int reportId, string adminUserId)
        {
            try
            {
                var report = await _unitOfWork.CommentReports.GetByIdAsync(reportId);
                if (report == null || report.Status != ReportStatus.Pending) return false;

                report.Status = ReportStatus.Reviewed; // Mark as reviewed, not affecting the comment's blocked status directly
                report.ReviewedByAdminId = adminUserId;
                report.ReviewedDate = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Report {ReportId} dismissed by Admin {AdminId}", reportId, adminUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing Report {ReportId}", reportId);
                return false;
            }
        }
    }

    // Simple static helper for TempData, if you decide to use it for service layer messages.
    // This is a basic example; in a real app, you might use a more sophisticated notification system.
    public static class TempDataHelper
    {
        [ThreadStatic] // Important for web apps to keep it per-request
        private static Action<string>? _setWarningMessageAction;

        public static void Configure(Action<string> setWarningMessageAction)
        {
            _setWarningMessageAction = setWarningMessageAction;
        }

        public static void SetWarningMessage(string message)
        {
            _setWarningMessageAction?.Invoke(message);
        }
    }
}