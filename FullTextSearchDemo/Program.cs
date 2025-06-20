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

app.MapControllers();


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

app.MapGet("/blogs/contains/normalized", (string searchTerm, BlogsDbContext context) =>
{
    var blogs = context.BlogPosts
        .Where(b =>
            b.Title.ToLower().Contains(searchTerm) ||
            b.Excerpt.ToLower().Contains(searchTerm) ||
            b.Content.ToLower().Contains(searchTerm))
        .Select(b => new
        {
            b.Slug,
            b.Title,
            b.Excerpt,
            b.Date
        })
        .ToList();

    return blogs;
})


app.Run();


