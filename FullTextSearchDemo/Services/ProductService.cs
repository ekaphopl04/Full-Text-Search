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

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task SeedDatabaseAsync()
        {
            // Only seed if the database is empty
            if (!await _context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Laptop",
                        Description = "High-performance laptop with 16GB RAM and 512GB SSD",
                        Price = 1299.99m,
                        Category = "Electronics"
                    },
                    new Product
                    {
                        Name = "Smartphone",
                        Description = "Latest smartphone with 6.7-inch display and 128GB storage",
                        Price = 999.99m,
                        Category = "Electronics"
                    },
                    new Product
                    {
                        Name = "Coffee Maker",
                        Description = "Automatic coffee maker with built-in grinder",
                        Price = 149.99m,
                        Category = "Kitchen Appliances"
                    },
                    new Product
                    {
                        Name = "Running Shoes",
                        Description = "Lightweight running shoes with cushioned soles",
                        Price = 89.99m,
                        Category = "Sports"
                    },
                    new Product
                    {
                        Name = "Desk Chair",
                        Description = "Ergonomic office chair with lumbar support",
                        Price = 249.99m,
                        Category = "Furniture"
                    }
                };

                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
            }
        }
    }
}
