using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace FullTextSearchDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly BlogService _blogService;
        private readonly BlogsDbContext _dbContext;

        public ProductsController(BlogService blogService, BlogsDbContext dbContext)
        {
            _blogService = blogService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogPost>>> GetProducts()
        {
            var blogs = await _blogService.GetAllBlogsAsync();
            return Ok(blogs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BlogPost>> GetProduct(string id)
        {
            var blog = await _blogService.GetBlogBySlugAsync(id);
            
            if (blog == null)
            {
                return NotFound();
            }

            return Ok(blog);
        }
    }
}
