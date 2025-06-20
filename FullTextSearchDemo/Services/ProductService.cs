using FullTextSearchDemo.Data;
using FullTextSearchDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace FullTextSearchDemo.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllProductsAsync();
            }

            // Use the database function for full-text search
            return await _context.SearchProducts(searchTerm).ToListAsync();
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await GetProductByIdAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        
        public async Task SeedDatabaseAsync()
        {
            if (!await _context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    new Product { Name = "Smartphone", Description = "High-end smartphone with advanced features", Price = 999.99m, Category = "Electronics" },
                    new Product { Name = "Laptop", Description = "Powerful laptop for professional use", Price = 1499.99m, Category = "Electronics" },
                    new Product { Name = "Coffee Maker", Description = "Automatic coffee machine for home use", Price = 129.99m, Category = "Home Appliances" },
                    new Product { Name = "Running Shoes", Description = "Comfortable shoes for jogging and running", Price = 89.99m, Category = "Sports" },
                    new Product { Name = "Headphones", Description = "Wireless noise-cancelling headphones", Price = 199.99m, Category = "Electronics" },
                    new Product { Name = "Desk Chair", Description = "Ergonomic office chair with lumbar support", Price = 249.99m, Category = "Furniture" },
                    new Product { Name = "Blender", Description = "High-speed blender for smoothies and food processing", Price = 79.99m, Category = "Kitchen" },
                    new Product { Name = "Yoga Mat", Description = "Non-slip yoga mat for exercise and meditation", Price = 29.99m, Category = "Sports" },
                    new Product { Name = "Smart Watch", Description = "Fitness tracker and smartwatch with heart rate monitor", Price = 149.99m, Category = "Electronics" },
                    new Product { Name = "Desk Lamp", Description = "LED desk lamp with adjustable brightness", Price = 39.99m, Category = "Home" }
                };
                
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
            }
        }
    }
}
