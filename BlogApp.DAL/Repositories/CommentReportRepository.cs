using BlogApp.Core.Entities;
using BlogApp.DAL.Data;
using BlogApp.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace BlogApp.DAL.Repositories
{
    public class CommentReportRepository : Repository<CommentReport>, ICommentReportRepository
    {
        public CommentReportRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<CommentReport>> GetPendingReportsWithDetailsAsync()
        {
            return await _context.CommentReports
                                 .Where(cr => cr.Status == ReportStatus.Pending)
                                 .Include(cr => cr.Comment)
                                     .ThenInclude(c => c.Article)
                                 .Include(cr => cr.ReporterUser)
                                 .OrderByDescending(cr => cr.ReportDate)
                                 .ToListAsync();
        }

        public async Task<CommentReport?> GetReportByIdWithDetailsAsync(int reportId)
        {
            return await _context.CommentReports
                                .Include(cr => cr.Comment)
                                    .ThenInclude(c => c.Article)
                                .Include(cr => cr.ReporterUser)
                                .Include(cr => cr.ReviewedByAdmin)
                                .FirstOrDefaultAsync(cr => cr.Id == reportId);
        }

        public async Task<bool> HasUserReportedCommentAsync(int commentId, string userId)
        {
            return await _context.CommentReports
                                .AnyAsync(cr => cr.CommentId == commentId && cr.ReporterUserId == userId);
        }
    }
}