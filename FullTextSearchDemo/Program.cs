using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // Changed to support MVC with views
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL with EF Core
builder.Services.AddDbContext<BlogsDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(Console.WriteLine, LogLevel.Information);
});

// Register services
builder.Services.AddScoped<BlogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Configure routing for MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Set up synonym dictionary for full-text search
SetupSynonymDictionary(app);

// Basic search using LIKE/Contains
app.MapGet("/blogs/contains", (string searchTerm, BlogsDbContext context) =>
{
    var blogs = context.BlogPosts
        .Where(b =>
            b.Title.Contains(searchTerm) ||
            b.Excerpt.Contains(searchTerm) ||
            b.Content.Contains(searchTerm))
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Date
        })
        .ToList();

    return blogs;
});

// Case-insensitive search using LIKE/Contains
app.MapGet("/blogs/contains/normalized", (string searchTerm, BlogsDbContext context) =>
{
    var blogs = context.BlogPosts
        .Where(b =>
            b.Title.ToLower().Contains(searchTerm.ToLower()) ||
            b.Excerpt.ToLower().Contains(searchTerm.ToLower()) ||
            b.Content.ToLower().Contains(searchTerm.ToLower()))
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Date
        })
        .ToList();

    return blogs;
});

// Basic search using LIKE/Contains
app.MapGet("/blogs/full-text", (string searchTerm, BlogsDbContext context) =>
{
    var blogs = context.BlogPosts
        .Where(b =>
            EF.Functions.ToTsVector("english", b.Title + " " + b.Excerpt + " " + b.Content).Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Date
        })
        .ToList();

    return blogs;
});

// Basic search using LIKE/Contains
app.MapGet("/blogs/vector/full-text/ranking", (string searchTerm, BlogsDbContext context) =>
{
    var blogs = context.BlogPostVectors
        .Where(b =>
            b.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Date,
            Rank = b.SearchVector
            .Rank(EF.Functions.PhraseToTsQuery("english", searchTerm))
        })
        .OrderByDescending(b => b.Rank)
        .ToList();

    return blogs;
});


app.Run();

// Method to set up the synonym dictionary in PostgreSQL
void SetupSynonymDictionary(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogsDbContext>();
        var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection;
        
        // Copy the synonyms file to PostgreSQL's data directory
        var synonymsFilePath = Path.Combine(app.Environment.ContentRootPath, "wordnet_synonyms.syn");
        if (File.Exists(synonymsFilePath))
        {
            // Execute the SQL script to set up the synonym dictionary
            var sqlScript = File.ReadAllText(Path.Combine(app.Environment.ContentRootPath, "Database", "SetupSynonyms.sql"));
            
            connection?.Open();
            using var command = connection?.CreateCommand();
            if (command != null)
            {
                command.CommandText = sqlScript;
                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Synonym dictionary set up successfully.");
                }
                catch (PostgresException ex)
                {
                    // If the dictionary already exists, this is fine
                    if (ex.SqlState == "42710") // duplicate_object
                    {
                        Console.WriteLine("Synonym dictionary already exists.");
                    }
                    else
                    {
                        Console.WriteLine($"Error setting up synonym dictionary: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"Synonym file not found at: {synonymsFilePath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error setting up synonym dictionary: {ex.Message}");
    }
}
