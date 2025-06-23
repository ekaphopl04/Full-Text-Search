using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace FullTextSearchDemo.Controllers
{
    [ApiController]
    [Route("api/blogs")]
    public class BlogsApiController : ControllerBase
    {
        private readonly BlogService _blogService;
        private readonly BlogsDbContext _context;

        public BlogsApiController(BlogService blogService, BlogsDbContext context)
        {
            _blogService = blogService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPost>>> GetBlogs()
        {
            var blogs = await _blogService.GetAllBlogsAsync();
            return Ok(blogs);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<BlogPost>> GetBlog(string slug)
        {
            var blog = await _blogService.GetBlogBySlugAsync(slug);

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
            var blogs = _context.BlogPosts
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

            return Ok(blogs);
        }

        // Case-insensitive search using LIKE/Contains
        [HttpGet("contains/normalized")]
        public IActionResult SearchContainsNormalized([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPosts
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

            return Ok(blogs);
        }

        // Full-text search
        [HttpGet("full-text")]
        public IActionResult SearchFullText([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPosts
                .Where(b =>
                    EF.Functions.ToTsVector("english", b.Title + " " + b.Excerpt + " " + b.Content)
                    .Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Excerpt,
                    b.Date
                })
                .ToList();

            return Ok(blogs);
        }

        // Full-text search with ranking
        [HttpGet("vector/full-text/ranking")]
        public IActionResult SearchFullTextWithRanking([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostVectors
                .Where(b =>
                    b.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Excerpt,
                    b.Date,
                    Rank = b.SearchVector.Rank(EF.Functions.PhraseToTsQuery("english", searchTerm))
                })
                .OrderByDescending(b => b.Rank)
                .ToList();

            return Ok(blogs);
        }
    }
}
