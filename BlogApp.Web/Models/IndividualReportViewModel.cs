using BlogApp.Core.Entities;
using System;

namespace BlogApp.Web.Models
{
    public class IndividualReportViewModel
    {
        public int ReportId { get; set; }
        public string ReporterUsername { get; set; }
        public DateTime ReportDate { get; set; }
        public string? Reason { get; set; }
        public ReportStatus Status { get; set; }
        public string? ReviewedByAdminUsername { get; set; }
        public DateTime? ReviewedDate { get; set; }
    }
}