using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.RegularExpressions;
using Npgsql;

namespace FullTextSearchDemo.Controllers
{
    [ApiController]
    [Route("api/blogs-thai")]
    public class BlogsThaiApiController : ControllerBase
    {
        private readonly BlogsDbContext _context;

        public BlogsThaiApiController(BlogsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPostThai>>> GetBlogs()
        {
            var blogs = await _context.BlogPostsThai.ToListAsync();
            return Ok(blogs);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<BlogPostThai>> GetBlog(string slug)
        {
            var blog = await _context.BlogPostsThai.FirstOrDefaultAsync(b => b.Slug == slug);

            if (blog == null)
            {
                return NotFound();
            }

            return Ok(blog);
        }

        // Basic search using LIKE/Contains
        [HttpGet("contains")]
        public IActionResult SearchContains([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostsThai
                .Where(b =>
                    b.Title.Contains(searchTerm) ||
                    b.Excerpt.Contains(searchTerm) ||
                    b.Content.Contains(searchTerm))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
                    b.Excerpt,
                    b.Date,
                    b.Position,
                    b.Page
                })
                .ToList();

            return Ok(blogs);
        }

        // Case-insensitive search using LIKE/Contains
        [HttpGet("contains/normalized")]
        public IActionResult SearchContainsNormalized([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostsThai
                .Where(b =>
                    b.Title.ToLower().Contains(searchTerm.ToLower()) ||
                    b.Excerpt.ToLower().Contains(searchTerm.ToLower()) ||
                    b.Content.ToLower().Contains(searchTerm.ToLower()))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
                    b.Excerpt,
                    b.Date,
                    b.Position,
                    b.Page
                })
                .ToList();

            return Ok(blogs);
        }

        // Full-text search
        [HttpGet("full-text")]
        public IActionResult SearchFullText([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostsThai
                .Where(b =>
                    EF.Functions.ToTsVector("simple", b.Title + " " + b.Excerpt + " " + b.Content)
                    .Matches(EF.Functions.PhraseToTsQuery("simple", searchTerm)))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
                    b.Excerpt,
                    b.Date,
                    b.Position,
                    b.Page
                })
                .ToList();

            return Ok(blogs);
        }

        // Full-text search with headline highlighting
        [HttpGet("full-text/highlight")]
        public IActionResult SearchFullTextHighlight([FromQuery] string searchTerm)
        {
            var formattedSearchTerm = searchTerm.Replace(" ", "%").ToLower();

            // First, get the matching blogs from database
            var matchingBlogs = _context.BlogPostsThai
                .Where(b =>
                   EF.Functions.ILike(b.Slug.ToLower(), $"%{formattedSearchTerm}%") ||
                    EF.Functions.ILike(b.Title.ToLower(), $"%{formattedSearchTerm}%") ||
                    EF.Functions.ILike(b.Excerpt.ToLower(), $"%{formattedSearchTerm}%") ||
                    EF.Functions.ILike(b.Content.ToLower(), $"%{formattedSearchTerm}%"))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
                    b.Excerpt,
                    b.Date,
                    b.Position,
                    b.Page
                })
                .ToList();

            // Then apply highlighting on the client side
            var blogs = matchingBlogs
                .Select(b => new
                {
                    Slug = Regex.Replace(b.Slug, searchTerm, m => $"<yellow>{m.Value}</yellow>", RegexOptions.IgnoreCase),
                    Title = Regex.Replace(b.Title, searchTerm, m => $"<yellow>{m.Value}</yellow>", RegexOptions.IgnoreCase),
                    Content = Regex.Replace(b.Content, searchTerm, m => $"<yellow>{m.Value}</yellow>", RegexOptions.IgnoreCase),
                    Excerpt = Regex.Replace(b.Excerpt, searchTerm, m => $"<yellow>{m.Value}</yellow>", RegexOptions.IgnoreCase),
                    b.Date,
                    b.Position,
                    b.Page
                })
                .ToList();

            return Ok(blogs);
        }
    }
}
