using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Models;
using FullTextSearchDemo.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure PostgreSQL with EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=fulltext_search_demo;Username=postgres;Password=postgres"));
        
        // Register our ProductService
        services.AddScoped<ProductService>();
    })
    .Build();

// Get the service provider
using var serviceScope = host.Services.CreateScope();
var serviceProvider = serviceScope.ServiceProvider;

// Get the database context
var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

// Ensure the database is created and migrations are applied
dbContext.Database.Migrate();

// Get the product service
var productService = serviceProvider.GetRequiredService<ProductService>();

// Add some sample products if none exist
if (!await dbContext.Products.AnyAsync())
{
    Console.WriteLine("Adding sample products...");
    
    await productService.AddProductAsync(new Product
    {
        Name = "iPhone 15 Pro",
        Description = "Apple's latest flagship smartphone with A17 Pro chip and advanced camera system",
        Price = 999.99m,
        Category = "Smartphones"
    });
    
    await productService.AddProductAsync(new Product
    {
        Name = "Samsung Galaxy S24 Ultra",
        Description = "Samsung's premium smartphone with S Pen support and powerful camera capabilities",
        Price = 1199.99m,
        Category = "Smartphones"
    });
    
    await productService.AddProductAsync(new Product
    {
        Name = "MacBook Pro 16",
        Description = "High-performance laptop with M3 Pro or M3 Max chip for professional workflows",
        Price = 2499.99m,
        Category = "Laptops"
    });
    
    await productService.AddProductAsync(new Product
    {
        Name = "Dell XPS 15",
        Description = "Premium Windows laptop with InfinityEdge display and powerful Intel processors",
        Price = 1799.99m,
        Category = "Laptops"
    });
    
    await productService.AddProductAsync(new Product
    {
        Name = "iPad Air",
        Description = "Versatile tablet with M2 chip for productivity and entertainment",
        Price = 599.99m,
        Category = "Tablets"
    });
    
    Console.WriteLine("Sample products added successfully!");
}

// Demonstrate full-text search
Console.WriteLine("\nFull-Text Search Demo\n");

while (true)
{
    Console.Write("Enter search term (or 'exit' to quit): ");
    var searchTerm = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.ToLower() == "exit")
    {
        break;
    }
    
    // Format the search term for PostgreSQL full-text search
    // Replace spaces with & for AND operations in tsquery
    var formattedSearchTerm = string.Join(" & ", searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    
    Console.WriteLine($"\nSearching for: {searchTerm} (formatted as: {formattedSearchTerm})\n");
    
    var results = await productService.SearchProductsAsync(formattedSearchTerm);
    
    if (results.Any())
    {
        Console.WriteLine($"Found {results.Count} results:\n");
        
        foreach (var product in results)
        {
            Console.WriteLine($"ID: {product.Id}");
            Console.WriteLine($"Name: {product.Name}");
            Console.WriteLine($"Description: {product.Description}");
            Console.WriteLine($"Price: ${product.Price}");
            Console.WriteLine($"Category: {product.Category}");
            Console.WriteLine(new string('-', 50));
        }
    }
    else
    {
        Console.WriteLine("No results found.");
    }
    
    Console.WriteLine();
}

Console.WriteLine("Thank you for using Full-Text Search Demo!");
