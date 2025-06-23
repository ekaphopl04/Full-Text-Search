using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;

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
    }
}
