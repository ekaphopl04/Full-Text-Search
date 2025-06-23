using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FullTextSearchDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlogsDbContext _context;
        private readonly BlogService _blogService;

        public HomeController(BlogsDbContext context, BlogService blogService)
        {
            _context = context;
            _blogService = blogService;
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
        public IActionResult SearchResults(string searchTerm, string searchType = "basic")
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SearchType = searchType;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ViewBag.Results = new List<object>();
                return View();
            }

            if (searchType == "fulltext")
            {
                var results = _context.BlogPostVectors
                    .Where(b => b.SearchVector.Matches(EF.Functions.PhraseToTsQuery("english", searchTerm)))
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

                ViewBag.Results = results;
            }
            else
            {
                var results = _context.BlogPosts
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

                ViewBag.Results = results;
            }

            return View();
        }
    }
}
