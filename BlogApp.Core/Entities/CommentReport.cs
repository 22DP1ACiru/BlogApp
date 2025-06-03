using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Core.Entities
{
    /// <summary>
    /// Represents the status of a comment report.
    /// </summary>
    public enum ReportStatus
    {
        /// <summary>
        /// The report is awaiting review by an administrator.
        /// </summary>
        Pending,

        /// <summary>
        /// The report has been reviewed by an administrator, and no blocking action was taken on the comment for this report.
        /// (e.g., report dismissed as invalid).
        /// </summary>
        Reviewed,

        /// <summary>
        /// The report has been actioned by an administrator blocking the associated comment.
        /// </summary>
        Blocked
    }

    /// <summary>
    /// Represents a report submitted by a user against a comment.
    /// </summary>
    public class CommentReport
    {
        /// <summary>
        /// Gets or sets the unique identifier for the report.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the Comment that is being reported.
        /// </summary>
        [Required]
        public int CommentId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the ApplicationUser who submitted this report.
        /// </summary>
        [Required]
        public string ReporterUserId { get; set; }

        /// <summary>
        /// Gets or sets the reason provided by the user for reporting the comment. Optional.
        /// </summary>
        [StringLength(500, ErrorMessage = "Report reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the report was submitted.
        /// </summary>
        public DateTime ReportDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the report.
        /// Defaults to Pending.
        /// </summary>
        [Required]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        /// <summary>
        /// Gets or sets the foreign key to the ApplicationUser (Administrator) who reviewed this report.
        /// Null if the report has not yet been reviewed.
        /// </summary>
        public string? ReviewedByAdminId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the report was reviewed.
        /// Null if the report has not yet been reviewed.
        /// </summary>
        public DateTime? ReviewedDate { get; set; }

        // Navigation Properties
        /// <summary>
        /// Navigation property for the reported Comment.
        /// </summary>
        [ForeignKey(nameof(CommentId))]
        public virtual Comment Comment { get; set; }

        /// <summary>
        /// Navigation property for the User who submitted the report.
        /// </summary>
        [ForeignKey(nameof(ReporterUserId))]
        public virtual ApplicationUser ReporterUser { get; set; }

        /// <summary>
        /// Navigation property for the Administrator who reviewed the report.
        /// </summary>
        [ForeignKey(nameof(ReviewedByAdminId))]
        public virtual ApplicationUser? ReviewedByAdmin { get; set; }
    }
}