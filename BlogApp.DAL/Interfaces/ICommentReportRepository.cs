using BlogApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    public interface ICommentReportRepository : IRepository<CommentReport>
    {
        Task<IEnumerable<CommentReport>> GetPendingReportsWithDetailsAsync();
        Task<CommentReport?> GetReportByIdWithDetailsAsync(int reportId);
        Task<bool> HasUserReportedCommentAsync(int commentId, string userId);
    }
}