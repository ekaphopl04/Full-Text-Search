using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
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
                    EF.Functions.ToTsVector("thai", b.Title + " " + b.Excerpt + " " + b.Content)
                    .Matches(EF.Functions.PhraseToTsQuery("thai", searchTerm)))
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
            // Format the search term for PostgreSQL full-text search
            var formattedSearchTerm = searchTerm.Replace(" ", " | ");

            // Use raw SQL to get highlighting with ts_headline
            var sql = @"
                SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"", ""Position"", ""Page"",
                       ts_headline('thai', ""Slug"", to_tsquery('thai', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightSlug,
                       ts_headline('thai', ""Title"", to_tsquery('thai', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightTitle,
                       ts_headline('thai', ""Content"", to_tsquery('thai', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightContent,
                       ts_headline('thai', ""Excerpt"", to_tsquery('thai', @p0), 'MaxWords=50, MinWords=10, ShortWord=3, HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightExcerpt
                FROM (
                    SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"", ""Position"", ""Page"",
                    to_tsvector('thai', ""Slug"" || ' ' || ""Title"" || ' ' || ""Excerpt"" || ' ' || ""Content"" || ' ' || ""Date"") AS SearchVector
                    FROM ""BlogPostsThai""
                ) AS BlogPostsThai
                WHERE SearchVector @@ to_tsquery('thai', @p0)";

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
                    Slug = reader.GetString(0),           // Slug
                    Title = reader.GetString(1),          // Title
                    Content = reader.GetString(2),        // Content
                    Excerpt = reader.GetString(3),        // Excerpt
                    Date = reader.GetString(4),           // Date
                    Position = reader.GetString(5),       // Position
                    Page = reader.GetString(6),           // Page
                    HighlightSlug = reader.GetString(7),      // HighlightSlug
                    HighlightTitle = reader.GetString(8),     // HighlightTitle
                    HighlightContent = reader.GetString(9),   // HighlightContent
                    HighlightExcerpt = reader.GetString(10)   // HighlightExcerpt
                });
            }

            return Ok(results);
        }

        // Full-text search with ranking
        [HttpGet("full-text/ranking")]
        public IActionResult SearchFullTextWithRanking([FromQuery] string searchTerm)
        {
            // Format the search term for PostgreSQL full-text search
            var formattedSearchTerm = searchTerm.Replace(" ", " | ");

            // Use raw SQL for ranking with ts_rank
            var sql = @"
                SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"", ""Position"", ""Page"",
                       ts_rank(to_tsvector('thai', ""Title"" || ' ' || ""Excerpt"" || ' ' || ""Content""), to_tsquery('thai', @p0)) AS Rank
                FROM ""BlogPostsThai""
                WHERE to_tsvector('thai', ""Title"" || ' ' || ""Excerpt"" || ' ' || ""Content"") @@ to_tsquery('thai', @p0)
                ORDER BY Rank DESC";

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
                    Position = reader.GetString(5),   // Position
                    Page = reader.GetString(6),       // Page
                    Rank = reader.GetDouble(7)        // Rank
                });
            }

            return Ok(results);
        }
    }
}
