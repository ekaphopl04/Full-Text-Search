using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FullTextSearchDemo.Services
{
    public class BlogService
    {
        private readonly BlogsDbContext _context;

        public BlogService(BlogsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BlogPost>> GetAllBlogsAsync()
        {
            return await _context.BlogPosts.ToListAsync();
        }

        public async Task<BlogPost?> GetBlogBySlugAsync(string slug)
        {
            return await _context.BlogPosts.FirstOrDefaultAsync(b => b.Slug == slug);
        }

        public async Task<BlogPost> AddBlogAsync(BlogPost blog)
        {
            _context.BlogPosts.Add(blog);
            await _context.SaveChangesAsync();
            return blog;
        }

        public async Task<IEnumerable<BlogPost>> SearchBlogsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllBlogsAsync();

            searchTerm = searchTerm.ToLower();
            
            return await _context.BlogPosts
                .Where(b => 
                    b.Title.ToLower().Contains(searchTerm) || 
                    b.Excerpt.ToLower().Contains(searchTerm) || 
                    b.Content.ToLower().Contains(searchTerm))
                .ToListAsync();
        }

        public async Task SeedBlogsAsync()
        {
            // Only seed if the database is empty
            if (!await _context.BlogPosts.AnyAsync())
            {
                var blogs = new List<BlogPost>
                {
                    new BlogPost
                    {
                        Slug = "getting-started-with-full-text-search",
                        Title = "Getting Started with Full-Text Search",
                        Excerpt = "Learn how to implement full-text search in your applications",
                        Content = "Full-text search is a powerful technique for searching large amounts of text data efficiently. This post covers the basics of implementing full-text search in your applications.",
                        Date = "September 15, 2023"
                    },
                    new BlogPost
                    {
                        Slug = "advanced-search-techniques",
                        Title = "Advanced Search Techniques",
                        Excerpt = "Explore advanced techniques for implementing search functionality",
                        Content = "This post explores advanced search techniques including fuzzy matching, relevance scoring, and search result highlighting to enhance your application's search capabilities.",
                        Date = "September 15, 2023"
                    },
                    new BlogPost
                    {
                        Slug = "optimizing-postgresql-for-search",
                        Title = "Optimizing PostgreSQL for Search",
                        Excerpt = "Tips and tricks for optimizing PostgreSQL for full-text search",
                        Content = "PostgreSQL offers powerful full-text search capabilities. This post covers how to optimize your PostgreSQL database for efficient full-text search operations.",
                        Date = "September 15, 2023"
                    },
                    new BlogPost
                    {
                        Slug = "search-ui-best-practices",
                        Title = "Search UI Best Practices",
                        Excerpt = "Design tips for creating effective search interfaces",
                        Content = "A good search interface can make or break your application. This post covers best practices for designing search interfaces that are both effective and user-friendly.",
                        Date = "September 15, 2023"
                    }
                };

                await _context.BlogPosts.AddRangeAsync(blogs);
                await _context.SaveChangesAsync();
            }
        }
    }
}
