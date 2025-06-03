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
            if (commentId <= 0)
            {
                _logger.LogWarning("ReportCommentAsync: Invalid CommentId {CommentId} provided by User {ReporterUserId}.", commentId, reporterUserId);
                return false;
            }
            if (string.IsNullOrWhiteSpace(reporterUserId))
            {
                _logger.LogWarning("ReportCommentAsync: Null or empty ReporterUserId provided for CommentId {CommentId}.", commentId);
                return false;
            }

            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null)
            {
                _logger.LogWarning("Failed to report: Comment {CommentId} not found. Reported by User {ReporterUserId}.", commentId, reporterUserId);
                return false;
            }

            if (comment.UserId == reporterUserId)
            {
                _logger.LogInformation("User {ReporterUserId} attempted to report their own comment (ID: {CommentId}). Action disallowed.", reporterUserId, commentId);
                return false;
            }

            if (await _unitOfWork.CommentReports.HasUserPendingReportForCommentAsync(commentId, reporterUserId))
            {
                _logger.LogInformation("User {ReporterUserId} already has a PENDING report for Comment {CommentId}. New report not created.", reporterUserId, commentId);
                return false;
            }

            try
            {
                var report = new CommentReport
                {
                    CommentId = commentId,
                    ReporterUserId = reporterUserId,
                    Reason = reason?.Trim(),
                    ReportDate = DateTime.UtcNow,
                    Status = ReportStatus.Pending
                };

                await _unitOfWork.CommentReports.AddAsync(report);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} successfully reported by User {ReporterUserId}. Report ID: {ReportId}.", commentId, reporterUserId, report.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting Comment {CommentId} by User {ReporterUserId}. Reason: {ReportReason}", commentId, reporterUserId, reason);
                return false;
            }
        }

        public async Task<IEnumerable<GroupedCommentReportDto>> GetGroupedPendingReportsAsync()
        {
            try
            {
                var pendingReports = await _unitOfWork.CommentReports.GetPendingReportsWithDetailsAsync();

                if (!pendingReports.Any())
                {
                    return Enumerable.Empty<GroupedCommentReportDto>();
                }

                var groupedReportDtos = pendingReports
                    .GroupBy(r => r.CommentId) // Group by the comment that was reported
                    .Select(group =>
                    {
                        var firstReportInGroup = group.First();
                        var comment = firstReportInGroup.Comment;

                        if (comment == null)
                        {
                            _logger.LogWarning("CommentReport (e.g., ID {ReportId}) is associated with a deleted or non-existent CommentId {CommentId}. Skipping this group.", firstReportInGroup.Id, firstReportInGroup.CommentId);
                            return null;
                        }

                        return new GroupedCommentReportDto
                        {
                            CommentId = comment.Id,
                            CommentContentPreview = comment.Content?.Length > 100 ? comment.Content.Substring(0, 100) + "..." : (comment.Content ?? "[Content Missing]"),
                            FullCommentContent = comment.Content ?? "[Content Missing]",
                            ArticleId = comment.ArticleId,
                            ArticleTitle = comment.Article?.Title ?? "[Article Deleted/Missing]", // Handle if article also deleted
                            IsCommentBlocked = comment.IsBlocked,
                            PendingReportCount = group.Count(r => r.Status == ReportStatus.Pending), // Count only PENDING reports
                            IndividualReportDetails = group
                                .Where(r => r.Status == ReportStatus.Pending) // Include details of PENDING reports only
                                .Select(r => new ReportDetailDto
                                {
                                    ReportId = r.Id,
                                    ReporterUsername = r.ReporterUser?.UserName ?? "[Reporter User Deleted/Missing]",
                                    ReportDate = r.ReportDate,
                                    Reason = r.Reason,
                                    Status = r.Status
                                })
                                .OrderByDescending(ir => ir.ReportDate) // Show newest reports first within the group
                                .ToList()
                        };
                    })
                    .Where(dto => dto != null && dto.PendingReportCount > 0) // Filter out groups for deleted comments or no pending reports
                    .OrderByDescending(dto => dto!.IndividualReportDetails.Any() ? dto.IndividualReportDetails.Max(ir => ir.ReportDate) : DateTime.MinValue) // Order groups by newest report
                    .ToList();

                return groupedReportDtos!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving and grouping pending comment reports.");
                return Enumerable.Empty<GroupedCommentReportDto>();
            }
        }


        public async Task<bool> BlockCommentAsync(int commentId, string adminUserId)
        {
            if (commentId <= 0 || string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("BlockCommentAsync: Invalid parameters. CommentId: {CommentId}, AdminUserId: '{AdminUserId}'.", commentId, adminUserId);
                return false;
            }

            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("BlockComment: Comment {CommentId} not found. Action by Admin {AdminUserId}.", commentId, adminUserId);
                    return false;
                }
                if (comment.IsBlocked)
                {
                    _logger.LogInformation("BlockComment: Comment {CommentId} is already blocked. Action by Admin {AdminUserId}.", commentId, adminUserId);
                    return false;
                }

                comment.IsBlocked = true;
                comment.LastUpdatedDate = DateTime.UtcNow; // Record when it was blocked

                var reportsToUpdate = await _unitOfWork.CommentReports.FindAsync(r => r.CommentId == commentId && r.Status == ReportStatus.Pending);
                int updatedReportsCount = 0;
                foreach (var report in reportsToUpdate)
                {
                    report.Status = ReportStatus.Blocked;
                    report.ReviewedByAdminId = adminUserId;
                    report.ReviewedDate = DateTime.UtcNow;
                    updatedReportsCount++;
                }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} blocked by Admin {AdminUserId}. {UpdatedReportsCount} pending reports marked as '{NewStatus}'.",
                    commentId, adminUserId, updatedReportsCount, ReportStatus.Blocked.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking Comment {CommentId} by Admin {AdminUserId}.", commentId, adminUserId);
                return false;
            }
        }

        public async Task<bool> UnblockCommentAsync(int commentId, string adminUserId)
        {
            if (commentId <= 0 || string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("UnblockCommentAsync: Invalid parameters. CommentId: {CommentId}, AdminUserId: '{AdminUserId}'.", commentId, adminUserId);
                return false;
            }
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null)
                {
                    _logger.LogWarning("UnblockComment: Comment {CommentId} not found. Action by Admin {AdminUserId}.", commentId, adminUserId);
                    return false;
                }
                if (!comment.IsBlocked)
                {
                    _logger.LogInformation("UnblockComment: Comment {CommentId} is not currently blocked. Action by Admin {AdminUserId}.", commentId, adminUserId);
                    return false;
                }

                comment.IsBlocked = false;
                comment.LastUpdatedDate = DateTime.UtcNow; 

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Comment {CommentId} unblocked by Admin {AdminUserId}.", commentId, adminUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking Comment {CommentId} by Admin {AdminUserId}.", commentId, adminUserId);
                return false;
            }
        }

        public async Task<bool> DismissReportAsync(int reportId, string adminUserId)
        {
            if (reportId <= 0 || string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("DismissReportAsync: Invalid parameters. ReportId: {ReportId}, AdminUserId: '{AdminUserId}'.", reportId, adminUserId);
                return false;
            }
            try
            {
                var report = await _unitOfWork.CommentReports.GetByIdAsync(reportId);
                if (report == null)
                {
                    _logger.LogWarning("DismissReport: Report {ReportId} not found. Action by Admin {AdminUserId}.", reportId, adminUserId);
                    return false;
                }
                if (report.Status != ReportStatus.Pending)
                {
                    _logger.LogInformation("DismissReport: Report {ReportId} is not pending (Status: {ReportStatus}). Action by Admin {AdminUserId}.", reportId, report.Status, adminUserId);
                    return false;
                }

                report.Status = ReportStatus.Reviewed;
                report.ReviewedByAdminId = adminUserId;
                report.ReviewedDate = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Report {ReportId} for Comment {CommentId} dismissed by Admin {AdminUserId}. Status changed to '{NewStatus}'.",
                    reportId, report.CommentId, adminUserId, report.Status.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing Report {ReportId} by Admin {AdminUserId}.", reportId, adminUserId);
                return false;
            }
        }
    }
}