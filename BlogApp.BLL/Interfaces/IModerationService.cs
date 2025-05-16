using BlogApp.Core.Entities;

namespace BlogApp.BLL.Interfaces
{
    public interface IModerationService
    {
        Task<bool> ReportCommentAsync(int commentId, string reporterUserId, string? reason);
        Task<IEnumerable<CommentReport>> GetPendingReportsAsync();
        Task<bool> BlockCommentAsync(int commentId, string adminUserId);
        Task<bool> UnblockCommentAsync(int commentId, string adminUserId);
        Task<bool> DismissReportAsync(int reportId, string adminUserId);
    }
}