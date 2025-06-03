using BlogApp.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleVote> ArticleVotes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReport> CommentReports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Article>().ToTable("Articles");

            builder.Entity<Article>().HasKey(a => a.Id);

            builder.Entity<Article>()
                .Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<Article>()
                .Property(a => a.ImageUrl)
                .HasMaxLength(500);

            builder.Entity<Article>()
                .HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<ArticleVote>().ToTable("ArticleVotes");

            builder.Entity<ArticleVote>().HasKey(av => av.Id);

            builder.Entity<ArticleVote>()
                .HasOne(av => av.Article)
                .WithMany()
                .HasForeignKey(av => av.ArticleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ArticleVote>()
                .HasOne(av => av.User)
                .WithMany()
                .HasForeignKey(av => av.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint to prevent multiple votes per user per article
            builder.Entity<ArticleVote>()
                .HasIndex(av => new { av.ArticleId, av.UserId })
                .IsUnique();


            builder.Entity<Comment>().ToTable("Comments");
            builder.Entity<Comment>().HasKey(c => c.Id);

            builder.Entity<Comment>()
                .Property(c => c.Content)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany()
                .HasForeignKey(c => c.ArticleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<CommentReport>().ToTable("CommentReports");

            builder.Entity<CommentReport>().HasKey(cr => cr.Id);

            builder.Entity<CommentReport>()
            .Property(cr => cr.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

            builder.Entity<CommentReport>()
                .Property(cr => cr.Reason)
                .HasMaxLength(500);

            // Relationships
            builder.Entity<CommentReport>()
                .HasOne(cr => cr.Comment)
                .WithMany()
                .HasForeignKey(cr => cr.CommentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CommentReport>()
                .HasOne(cr => cr.ReporterUser)
                .WithMany()
                .HasForeignKey(cr => cr.ReporterUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CommentReport>()
                .HasOne(cr => cr.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(cr => cr.ReviewedByAdminId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Removed unique index: cr.CommentId, cr.ReporterUserId
            // The logic in ModerationService.ReportCommentAsync already prevents
            // a user from creating a new report if they already have a PENDING one
            // for the same comment. Removing this DB constraint allows re-reporting
            // if the previous report was actioned (Reviewed/Blocked) and the comment
            // is problematic again.
            // builder.Entity<CommentReport>()
            //     .HasIndex(cr => new { cr.CommentId, cr.ReporterUserId })
            //     .IsUnique();
        }
    }
}