using BlogApp.Core.Entities;
using System;

namespace BlogApp.Web.Models
{
    public class CommentReportViewModel
    {
        public int ReportId { get; set; }
        public int CommentId { get; set; }
        public string ReportedCommentContent { get; set; }
        public string ReporterUsername { get; set; }
        public DateTime ReportDate { get; set; }
        public string? Reason { get; set; }
        public ReportStatus Status { get; set; }

        public int ArticleId { get; set; }
        public string ArticleTitle { get; set; }
    }
}