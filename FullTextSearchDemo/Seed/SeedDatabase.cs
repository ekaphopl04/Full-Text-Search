using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;

namespace FullTextSearchDemo.Seed
{
    internal sealed class SeedDatabase : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SeedDatabase(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<BlogsDbContext>();
            var blogsPostsJson = await File.ReadAllTextAsync("blogs-posts.json", stoppingToken);
            var blogsPosts = JsonSerializer.Deserialize<List<BlogPost>>(blogsPostsJson)!;
            
            if (!context.BlogPosts.Any())
            {
                context.BlogPosts.AddRange(blogsPosts);
            }

            if (!context.BlogPostVectors.Any())
            {
                context.BlogPostVectors.AddRange(blogsPosts.Select(b => new BlogPostVector
                {
                    Slug = b.Slug,
                    Title = b.Title,
                    Excerpt = b.Excerpt,
                    Content = b.Content,
                    Date = b.Date
                }));
            }
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
