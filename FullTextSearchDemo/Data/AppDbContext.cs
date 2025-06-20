using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FullTextSearchDemo.Models;
using System.Linq.Expressions;

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

            // Add a generated column for search vector that updates automatically
            modelBuilder.Entity<Product>()
                .Property(p => p.SearchVector)
                .HasComputedColumnSql(
                    "to_tsvector('english', coalesce(\"Name\",'') || ' ' || coalesce(\"Description\",'') || ' ' || coalesce(\"Category\",''))",
                    stored: true);

            // Register the full-text search function
            var searchProductsMethod = typeof(AppDbContext).GetMethod(nameof(SearchProducts), new[] { typeof(string) });
            if (searchProductsMethod != null)
            {
                modelBuilder.HasDbFunction(searchProductsMethod)
                    .HasName("search_products")
                    .HasTranslation(args =>
                    {
                        var searchTerm = args.First();
                        return new SqlFunctionExpression(
                            "ts_match_products",
                            new[] { searchTerm },
                            typeof(bool),
                            null);
                    });
            }
        }

        // Define a method that maps to a PostgreSQL function for full-text search
        public IQueryable<Product> SearchProducts(string searchTerm) =>
            FromExpression(() => SearchProducts(searchTerm));
    }
}
