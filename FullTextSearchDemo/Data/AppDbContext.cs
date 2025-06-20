using FullTextSearchDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace FullTextSearchDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure the Product entity
            modelBuilder.Entity<Product>()
                .HasKey(p => p.Id);
                
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired();
                
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
