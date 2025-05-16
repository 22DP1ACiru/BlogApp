using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Blocked
    }

    public class CommentReport
    {
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public string ReporterUserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime ReportDate { get; set; }

        [Required]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string? ReviewedByAdminId { get; set; }

        public DateTime? ReviewedDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CommentId))]
        public virtual Comment Comment { get; set; }

        [ForeignKey(nameof(ReporterUserId))]
        public virtual ApplicationUser ReporterUser { get; set; }

        [ForeignKey(nameof(ReviewedByAdminId))]
        public virtual ApplicationUser? ReviewedByAdmin { get; set; }
    }
}