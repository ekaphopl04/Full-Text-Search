using FullTextSearchDemo.Data;
using FullTextSearchDemo.Models;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL with EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<BlogService>();

// Configure PostgreSQL for BlogsDbContext
builder.Services.AddDbContext<BlogsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Add product search endpoint
app.MapGet("/products/search", async (string searchTerm, ProductService productService) =>
{
    var results = await productService.SearchProductsAsync(searchTerm);
    return results;
})
.WithName("SearchProducts")
.WithOpenApi();

// Apply migrations and seed data at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Migrate and seed AppDbContext
        var appDbContext = services.GetRequiredService<AppDbContext>();
        appDbContext.Database.Migrate();
        
        // Seed products
        var productService = services.GetRequiredService<ProductService>();
        await productService.SeedDatabaseAsync();
        
        // Migrate and seed BlogsDbContext
        var blogsDbContext = services.GetRequiredService<BlogsDbContext>();
        blogsDbContext.Database.Migrate();
        
        // Seed blogs
        var blogService = services.GetRequiredService<BlogService>();
        await blogService.SeedBlogsAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.MapGet("/weatherforecast", () =>
{
    string[] summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("blogs/contains", async (string searchTerm, BlogService blogService) =>
{
    var blogs = await blogService.SearchBlogsAsync(searchTerm);
    return blogs.Select(b => new
    {
        b.Slug,
        b.Title,
        b.Excerpt,
        b.Date
    });
});

// Advanced blog search with filtering and sorting
app.MapGet("blogs/search", async (string? searchTerm, string? category, string? sortBy, bool ascending = true, int? pageSize = 10, int? pageNumber = 1, BlogsDbContext context) =>
{
    // Start with all blog posts
    IQueryable<BlogPost> query = context.BlogPosts;
    
    // Apply search term filter if provided
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(b => 
            b.Title.Contains(searchTerm) || 
            b.Excerpt.Contains(searchTerm) || 
            b.Content.Contains(searchTerm));
    }
    
    // Apply category filter if provided
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(b => b.Category == category);
    }
    
    // Apply sorting
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        switch (sortBy.ToLower())
        {
            case "date":
                query = ascending ? query.OrderBy(b => b.Date) : query.OrderByDescending(b => b.Date);
                break;
            case "title":
                query = ascending ? query.OrderBy(b => b.Title) : query.OrderByDescending(b => b.Title);
                break;
            default:
                // Default sort by date
                query = ascending ? query.OrderBy(b => b.Date) : query.OrderByDescending(b => b.Date);
                break;
        }
    }
    else
    {
        // Default sort by date descending (newest first)
        query = query.OrderByDescending(b => b.Date);
    }
    
    // Apply pagination
    int size = pageSize ?? 10; // Default page size is 10
    int page = pageNumber ?? 1; // Default page number is 1
    
    // Calculate total count and pages
    int totalCount = await query.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)size);
    
    // Get paginated results
    var results = await query
        .Skip((page - 1) * size)
        .Take(size)
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Category,
            b.Date
        })
        .ToListAsync();
    
    // Return results with pagination metadata
    return new
    {
        TotalCount = totalCount,
        TotalPages = totalPages,
        CurrentPage = page,
        PageSize = size,
        Results = results
    };
})
.WithName("SearchBlogs")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
