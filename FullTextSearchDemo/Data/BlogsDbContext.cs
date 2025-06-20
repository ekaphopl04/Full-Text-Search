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

        public DbSet<BlogPostVector> BlogPostVectors { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
            modelBuilder.ApplyConfiguration(new BlogPostVectorConfiguration());
         
            base.OnModelCreating(modelBuilder);
        }
    }
}
