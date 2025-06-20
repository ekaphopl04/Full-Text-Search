using FullTextSearchDemo.Database;
using Microsoft.EntityFrameworkCore;

namespace FullTextSearchDemo.Data
{
    public class BlogsDbContext : DbContext
    {
        public BlogsDbContext(DbContextOptions<BlogsDbContext> options) : base(options)
        {
        }

        public DbSet<BlogPost> BlogPosts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure the BlogPost entity
            modelBuilder.Entity<BlogPost>()
                .HasKey(b => b.Slug);
                
            modelBuilder.Entity<BlogPost>()
                .Property(b => b.Slug)
                .IsRequired();
                
            modelBuilder.Entity<BlogPost>()
                .Property(b => b.Title)
                .IsRequired();
                
            // Create a full-text search index on Title, Excerpt, and Content
            modelBuilder.Entity<BlogPost>()
                .HasIndex(b => new { b.Title, b.Excerpt, b.Content });
        }
    }
}
