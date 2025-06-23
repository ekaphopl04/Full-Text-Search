using Microsoft.AspNetCore.Mvc;
using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using Microsoft.EntityFrameworkCore;

namespace FullTextSearchDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlogsDbContext _context;

        public HomeController(BlogsDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SearchResults(string searchTerm)
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

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Results = blogs;
            return View();
        }
    }
}
