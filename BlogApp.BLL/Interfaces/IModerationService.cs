using BlogApp.Core.Entities;
using BlogApp.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp.BLL.Interfaces
{
    public interface IModerationService
    {
        Task<bool> ReportCommentAsync(int commentId, string reporterUserId, string? reason);
        Task<IEnumerable<GroupedCommentReportDto>> GetGroupedPendingReportsAsync();
        Task<bool> BlockCommentAsync(int commentId, string adminUserId);
        Task<bool> UnblockCommentAsync(int commentId, string adminUserId);
        Task<bool> DismissReportAsync(int reportId, string adminUserId);
    }
}