using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Npgsql;

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
                    b.Content,
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
                    b.Content,
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
                    b.Content,
                    b.Excerpt,
                    b.Date
                })
                .ToList();

            return Ok(blogs);
        }
        // Full-text search with headline highlighting
        [HttpGet("full-text/highlight")]
        public IActionResult SearchFullTextHighlight([FromQuery] string searchTerm)
        {
            // Format the search term for PostgreSQL full-text search
            var formattedSearchTerm = searchTerm.Replace(" ", " | ");

            // Use raw SQL to get highlighting with ts_headline
            var sql = @"
                SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"",
                       ts_headline('wordnet_config', ""Slug"", to_tsquery('wordnet_config', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightSlug,
                       ts_headline('wordnet_config', ""Title"", to_tsquery('wordnet_config', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightTitle,
                       ts_headline('wordnet_config', ""Content"", to_tsquery('wordnet_config', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightContent,
                       ts_headline('wordnet_config', ""Excerpt"", to_tsquery('wordnet_config', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightExcerpt
                FROM (
                    SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"",
                    to_tsvector('wordnet_config', ""Slug"" || ' ' || ""Title"" || ' ' || ""Excerpt"" || ' ' || ""Content"" || ' ' || ""Date"") AS SearchVector
                    FROM ""BlogPosts""
                ) AS BlogPosts
                WHERE SearchVector @@ to_tsquery('wordnet_config', @p0)";

            using var connection = _context.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = new NpgsqlParameter("@p0", formattedSearchTerm);
            command.Parameters.Add(parameter);

            var results = new List<object>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new
                {
                    Slug = reader.GetString(0),       // Slug
                    Title = reader.GetString(1),      // Title
                    Content = reader.GetString(2),    // Content
                    Excerpt = reader.GetString(3),    // Excerpt
                    Date = reader.GetString(4),       // Date
                    HighlightSlug = reader.GetString(5),   // HighlightSlug
                    HighlightTitle = reader.GetString(6),   // HighlightTitle
                    HighlightContent = reader.GetString(7), // HighlightContent
                    HighlightExcerpt = reader.GetString(8)  // HighlightExcerpt
                });
            }

            return Ok(results);
        }

        // Full-text search with ranking
        [HttpGet("full-text/ranking")]
        public IActionResult SearchFullTextWithRanking([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPosts
                .Where(b =>
                    EF.Functions.ToTsVector("wordnet_config", b.Title + " " + b.Excerpt + " " + b.Content)
                    .Matches(EF.Functions.PhraseToTsQuery("wordnet_config", searchTerm)))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
                    b.Excerpt,
                    b.Date,
                    Rank = EF.Functions.ToTsVector("wordnet_config", b.Title + " " + b.Excerpt + " " + b.Content).Rank(EF.Functions.PhraseToTsQuery("wordnet_config", searchTerm))
                })
                .OrderByDescending(b => b.Rank)
                .ToList();

            return Ok(blogs);
        }

        // Full-text search with ranking
        [HttpGet("vector/full-text/ranking")]
        public IActionResult SearchFullVectorTextWithRanking([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostVectors
                .Where(b =>
                    b.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
                .Select(b => new
                {
                    b.Slug,
                    b.Title,
                    b.Content,
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
