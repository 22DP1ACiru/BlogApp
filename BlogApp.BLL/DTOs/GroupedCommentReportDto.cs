using BlogApp.Core.Entities;
using System.Collections.Generic;

namespace BlogApp.BLL.DTOs
{
    public class GroupedCommentReportDto
    {
        public int CommentId { get; set; }
        public string CommentContentPreview { get; set; }
        public string FullCommentContent { get; set; }
        public int ArticleId { get; set; }
        public string ArticleTitle { get; set; }
        public bool IsCommentBlocked { get; set; }
        public int PendingReportCount { get; set; }
        public List<ReportDetailDto> IndividualReportDetails { get; set; } = new List<ReportDetailDto>();
    }
}