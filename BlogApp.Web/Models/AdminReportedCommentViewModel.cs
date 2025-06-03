using BlogApp.Core.Entities;
using System.Collections.Generic;

namespace BlogApp.Web.Models
{
    public class AdminReportedCommentViewModel
    {
        public int CommentId { get; set; }
        public string ReportedCommentContentPreview { get; set; }
        public string FullCommentContent { get; set; }
        public int ArticleId { get; set; }
        public string ArticleTitle { get; set; }
        public bool IsCommentBlocked { get; set; }
        public int PendingReportCount { get; set; }
        public List<IndividualReportViewModel> IndividualReports { get; set; } = new List<IndividualReportViewModel>();
    }
}