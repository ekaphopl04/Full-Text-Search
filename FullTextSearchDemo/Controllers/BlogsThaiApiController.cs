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
                    EF.Functions.ToTsVector("thai", b.Title + " " + b.Excerpt + " " + b.Content)
                    .Matches(EF.Functions.ToTsQuery("thai", searchTerm)))
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
        
        // Get all available synonyms
        [HttpGet("synonyms")]
        public IActionResult GetSynonyms()
        {
            var synonymDictionary = new Dictionary<string, List<string>>();
            var synonymFilePath = Path.Combine(Directory.GetCurrentDirectory(), "thai_synonym.syn");
            
            if (System.IO.File.Exists(synonymFilePath))
            {
                var lines = System.IO.File.ReadAllLines(synonymFilePath);
                foreach (var line in lines)
                {
                    var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 1)
                    {
                        for (int i = 0; i < words.Length; i++)
                        {
                            if (!synonymDictionary.ContainsKey(words[i]))
                            {
                                synonymDictionary[words[i]] = new List<string>();
                            }
                            
                            // Add all other words as synonyms
                            for (int j = 0; j < words.Length; j++)
                            {
                                if (i != j && !synonymDictionary[words[i]].Contains(words[j]))
                                {
                                    synonymDictionary[words[i]].Add(words[j]);
                                }
                            }
                        }
                    }
                }
            }
            
            return Ok(synonymDictionary);
        }

        // Full-text search with auto synonyms
        [HttpGet("full-text/auto-synonyms")]
        public IActionResult SearchWithAutoSynonyms([FromQuery] string searchTerm)
        {
            // Read the synonym file to build a synonym dictionary
            var synonymDictionary = new Dictionary<string, List<string>>();
            var synonymFilePath = Path.Combine(Directory.GetCurrentDirectory(), "thai_synonym.syn");
            
            if (System.IO.File.Exists(synonymFilePath))
            {
                var lines = System.IO.File.ReadAllLines(synonymFilePath);
                foreach (var line in lines)
                {
                    var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 1)
                    {
                        for (int i = 0; i < words.Length; i++)
                        {
                            if (!synonymDictionary.ContainsKey(words[i]))
                            {
                                synonymDictionary[words[i]] = new List<string>();
                            }
                            
                            // Add all other words as synonyms
                            for (int j = 0; j < words.Length; j++)
                            {
                                if (i != j && !synonymDictionary[words[i]].Contains(words[j]))
                                {
                                    synonymDictionary[words[i]].Add(words[j]);
                                }
                            }
                        }
                    }
                }
            }
            
            // Split the search term into individual words
            var searchTerms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Collect all synonyms for each search term
            var allTermsWithSynonyms = new HashSet<string>();
            foreach (var term in searchTerms)
            {
                // Add the original term
                allTermsWithSynonyms.Add(term);
                
                // Add all synonyms if they exist
                if (synonymDictionary.TryGetValue(term, out var synonyms))
                {
                    foreach (var synonym in synonyms)
                    {
                        allTermsWithSynonyms.Add(synonym);
                    }
                }
            }
            
            // Format for tsquery: add :* to each term and join with OR (|)
            var formattedTerms = allTermsWithSynonyms.Select(term => term + ":*");
            var formattedSearchTerm = string.Join(" | ", formattedTerms);
            
            // If no terms were found, use the original search term
            if (string.IsNullOrEmpty(formattedSearchTerm))
            {
                formattedSearchTerm = searchTerm + ":*";
            }
            
            // Build the SQL query with all synonyms for both search and highlighting
            var sql = @"
                SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"", ""Position"", ""Page"",
                       ts_headline('thai_synonyms', ""Content"", to_tsquery('thai_synonyms', @p0), 
                                  'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightContent,
                       ts_headline('thai_synonyms', ""Title"", to_tsquery('thai_synonyms', @p0), 
                                  'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightTitle,
                       ts_headline('thai_synonyms', ""Excerpt"", to_tsquery('thai_synonyms', @p0), 
                                  'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightExcerpt,
                       ts_rank_cd(to_tsvector('thai_synonyms', ""Title"" || ' ' || ""Content"" || ' ' || ""Excerpt""), 
                                 to_tsquery('thai_synonyms', @p1)) AS Rank
                FROM ""BlogPostsThai""
                WHERE to_tsvector('thai_synonyms', ""Title"" || ' ' || ""Content"" || ' ' || ""Excerpt"") @@ 
                      to_tsquery('thai_synonyms', @p1)
                ORDER BY Rank DESC";

            using var connection = _context.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            // Add parameters for both the search and highlighting
            var parameter1 = new NpgsqlParameter("@p0", formattedSearchTerm);
            var parameter2 = new NpgsqlParameter("@p1", formattedSearchTerm);
            command.Parameters.Add(parameter1);
            command.Parameters.Add(parameter2);
            
            var results = new List<object>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new
                {
                    Slug = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Excerpt = reader.GetString(3),
                    Date = reader.GetString(4),
                    Position = reader.GetString(5),
                    Page = reader.GetString(6),
                    HighlightContent = reader.GetString(7),
                    HighlightTitle = reader.GetString(8),
                    HighlightExcerpt = reader.GetString(9),
                    Rank = reader.GetDouble(10)
                });
            }
            
            return Ok(results);
        }
        // Full-text search with headline highlighting for Thai language using tsvector with improved search
        [HttpGet("full-text/highlight-thai")]
        public IActionResult SearchFullTextHighlightThai([FromQuery] string searchTerm)
        {
            // แปลงคำค้นหาให้เหมาะสมกับภาษาไทยและใช้ prefix search
            // 1. แยกคำค้นหาตามช่องว่าง
            var searchTerms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 2. เพิ่ม :* ต่อท้ายแต่ละคำเพื่อให้เป็น prefix search (ค้นหาคำที่ขึ้นต้นด้วย)
            var formattedTerms = searchTerms.Select(term => term + ":*");

            // 3. เชื่อมคำด้วย | (OR) เพื่อให้ค้นหาคำใดคำหนึ่ง
            var formattedSearchTerm = string.Join(" | ", formattedTerms);

            // ถ้าไม่มีคำค้นหา ให้ใช้คำค้นหาเดิม
            if (string.IsNullOrEmpty(formattedSearchTerm))
            {
                formattedSearchTerm = searchTerm + ":*";
            }

            // ใช้ raw SQL เพื่อใช้ tsvector และ tsquery กับ prefix search
            var sql = @"
                SELECT ""Slug"", ""Title"", ""Content"", ""Excerpt"", ""Date"", ""Position"", ""Page"",
                       ts_headline('thai_synonyms', ""Content"", to_tsquery('thai_synonyms', @p0), 'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightContent,
                       ts_headline('thai_synonyms', ""Title"", to_tsquery('thai_synonyms', @p0), 'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightTitle,
                       ts_headline('thai_synonyms', ""Excerpt"", to_tsquery('thai_synonyms', @p0), 'HighlightAll=true, StartSel=<yellow>, StopSel=</yellow>') AS HighlightExcerpt,
                       ts_rank_cd(to_tsvector('thai_synonyms', ""Title"" || ' ' || ""Content"" || ' ' || ""Excerpt""), to_tsquery('thai_synonyms', @p0)) AS Rank
                FROM ""BlogPostsThai""
                WHERE 
                    to_tsvector('thai_synonyms', ""Title"" || ' ' || ""Content"" || ' ' || ""Excerpt"") @@ to_tsquery('thai_synonyms', @p0)
                ORDER BY Rank DESC";

            using var connection = _context.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            // พารามิเตอร์สำหรับ to_tsquery
            var parameter1 = new NpgsqlParameter("@p0", formattedSearchTerm);
            command.Parameters.Add(parameter1);

            var results = new List<object>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new
                {
                    Slug = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Excerpt = reader.GetString(3),
                    Date = reader.GetString(4),
                    Position = reader.GetString(5),
                    Page = reader.GetString(6),
                    HighlightContent = reader.GetString(7),
                    HighlightTitle = reader.GetString(8),
                    HighlightExcerpt = reader.GetString(9),
                    Rank = reader.GetDouble(10)
                });
            }

            return Ok(results);
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

        // Thai text search with word boundaries (for partial matches)
        [HttpGet("thai-word-search")]
        public IActionResult SearchThaiWords([FromQuery] string searchTerm)
        {
            var blogs = _context.BlogPostsThai
                .Where(b =>
                    EF.Functions.ILike(b.Title, $"%{searchTerm}%") ||
                    EF.Functions.ILike(b.Excerpt, $"%{searchTerm}%") ||
                    EF.Functions.ILike(b.Content, $"%{searchTerm}%"))
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
    }
}
