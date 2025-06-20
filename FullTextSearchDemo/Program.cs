using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Models;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// Configure PostgreSQL with EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=fulltext_search_demo;Username=postgres;Password=postgres"));

// Register our ProductService
builder.Services.AddScoped<ProductService>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Full-Text Search Demo API",
        Version = "v1",
        Description = "A simple API demonstrating full-text search capabilities with PostgreSQL",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Full-Text Search Demo API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add minimal API endpoints
app.MapGet("/api/products/search", async (string query, ProductService productService) =>
{
    var formattedSearchTerm = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    var results = await productService.SearchProductsAsync(formattedSearchTerm);
    return Results.Ok(results);
});

app.MapGet("/api/products", async (ProductService productService) =>
{
    var products = await productService.GetAllProductsAsync();
    return Results.Ok(products);
});

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
    var productService = serviceProvider.GetRequiredService<ProductService>();

    // Ensure the database is created and migrations are applied
    dbContext.Database.Migrate();

    // Add sample products if none exist
    if (!await dbContext.Products.AnyAsync())
    {
        await ProductSeeder.SeedSampleProductsAsync(productService);
    }
}

// Start the web application
app.Run();