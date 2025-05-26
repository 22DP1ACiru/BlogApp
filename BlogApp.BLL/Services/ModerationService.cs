using BlogApp.BLL.Interfaces;
using BlogApp.Core.Entities;
using BlogApp.DAL.Interfaces;
using BlogApp.BLL.DTOs;
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
                _logger.LogWarning("Failed to report: Comment {CommentId} not found.", commentId);
                return false;
            }

            if (comment.UserId == reporterUserId)
            {
                _logger.LogWarning("User {ReporterUserId} attempted to report their own comment {CommentId}.", reporterUserId, commentId);
                return false;
            }

            if (await _unitOfWork.CommentReports.HasUserPendingReportForCommentAsync(commentId, reporterUserId))
            {
                _logger.LogInformation("User {UserId} tried to report Comment {CommentId} again, but already has a PENDING report.", reporterUserId, commentId);
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
                    Status = ReportStatus.Pending
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

        public async Task<IEnumerable<GroupedCommentReportDto>> GetGroupedPendingReportsAsync()
        {
            try
            {
                var pendingReports = await _unitOfWork.CommentReports.GetPendingReportsWithDetailsAsync();
                if (pendingReports == null || !pendingReports.Any())
                {
                    return Enumerable.Empty<GroupedCommentReportDto>();
                }

                var groupedReportDtos = pendingReports
                    .GroupBy(r => r.CommentId)
                    .Select(group =>
                    {
                        var firstReportInGroup = group.First();
                        var comment = firstReportInGroup.Comment;

                        if (comment == null)
                        {
                            _logger.LogWarning("CommentReport {ReportId} associated with non-existent CommentId {CommentId}. Skipping.", firstReportInGroup.Id, firstReportInGroup.CommentId);
                            return null;
                        }

                        return new GroupedCommentReportDto
                        {
                            CommentId = comment.Id,
                            CommentContentPreview = comment.Content?.Length > 100 ? comment.Content.Substring(0, 100) + "..." : comment.Content ?? "[Content Missing]",
                            FullCommentContent = comment.Content ?? "[Content Missing]",
                            ArticleId = comment.ArticleId,
                            ArticleTitle = comment.Article?.Title ?? "[Article Deleted/Missing]",
                            IsCommentBlocked = comment.IsBlocked,
                            PendingReportCount = group.Count(r => r.Status == ReportStatus.Pending), // Ensure we only count pending
                            IndividualReportDetails = group.Where(r => r.Status == ReportStatus.Pending) // And only include pending details
                                                           .Select(r => new ReportDetailDto
                                                           {
                                                               ReportId = r.Id,
                                                               ReporterUsername = r.ReporterUser?.UserName ?? "Unknown User",
                                                               ReportDate = r.ReportDate,
                                                               Reason = r.Reason,
                                                               Status = r.Status
                                                           }).OrderByDescending(ir => ir.ReportDate).ToList()
                        };
                    })
                    .Where(dto => dto != null && dto.PendingReportCount > 0) // Ensure there are pending reports in the group
                    .OrderByDescending(dto => dto!.IndividualReportDetails.Any() ? dto.IndividualReportDetails.Max(ir => ir.ReportDate) : DateTime.MinValue)
                    .ToList();

                return groupedReportDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting and grouping pending reports for DTOs.");
                return Enumerable.Empty<GroupedCommentReportDto>();
            }
        }

        public async Task<bool> BlockCommentAsync(int commentId, string adminUserId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null || comment.IsBlocked)
                {
                    _logger.LogWarning("BlockComment: Comment {CommentId} not found or already blocked.", commentId);
                    return false;
                }

                comment.IsBlocked = true;
                comment.LastUpdatedDate = DateTime.UtcNow;

                var reports = await _unitOfWork.CommentReports.FindAsync(r => r.CommentId == commentId && r.Status == ReportStatus.Pending);
                foreach (var report in reports)
                {
                    report.Status = ReportStatus.Blocked; // Or "ActionedByBlock"
                    report.ReviewedByAdminId = adminUserId;
                    report.ReviewedDate = DateTime.UtcNow;
                }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} blocked by Admin {AdminId}. Pending reports marked as Blocked.", commentId, adminUserId);
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
                if (comment == null || !comment.IsBlocked)
                {
                    _logger.LogWarning("UnblockComment: Comment {CommentId} not found or not blocked.", commentId);
                    return false;
                }

                comment.IsBlocked = false;
                comment.LastUpdatedDate = DateTime.UtcNow;

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
                if (report == null || report.Status != ReportStatus.Pending)
                {
                    _logger.LogWarning("DismissReport: Report {ReportId} not found or not pending.", reportId);
                    return false;
                }

                report.Status = ReportStatus.Reviewed;
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

    // TempDataHelper can be removed if not used elsewhere, or kept if it is.
    // It's not directly related to the ViewModel/DTO refactoring.
    public static class TempDataHelper
    {
        [ThreadStatic]
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