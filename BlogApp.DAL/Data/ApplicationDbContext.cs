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
        }
    }
}