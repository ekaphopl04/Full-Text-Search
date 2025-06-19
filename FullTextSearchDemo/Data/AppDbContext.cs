using Microsoft.EntityFrameworkCore;
using FullTextSearchDemo.Models;

namespace FullTextSearchDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the full-text search index
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN"); // GIN index is optimized for full-text search

            // Create a migration SQL function to update the search vector
            var searchProductsMethod = typeof(AppDbContext).GetMethod(nameof(SearchProducts), new[] { typeof(string) });
            if (searchProductsMethod != null)
            {
                modelBuilder.HasDbFunction(searchProductsMethod).HasName("search_products");
            }
        }

        // Define a method that maps to a PostgreSQL function for full-text search
        public IQueryable<Product> SearchProducts(string searchTerm) =>
            FromExpression(() => SearchProducts(searchTerm));
    }
}
