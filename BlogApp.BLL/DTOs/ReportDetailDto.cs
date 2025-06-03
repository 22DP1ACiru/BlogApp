using BlogApp.Core.Entities;
using System;

namespace BlogApp.BLL.DTOs
{
    public class ReportDetailDto
    {
        public int ReportId { get; set; }
        public string ReporterUsername { get; set; }
        public DateTime ReportDate { get; set; }
        public string? Reason { get; set; }
        public ReportStatus Status { get; set; }
    }
}