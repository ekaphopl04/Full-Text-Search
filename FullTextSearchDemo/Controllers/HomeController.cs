using FullTextSearchDemo.Data;
using FullTextSearchDemo.Database;
using FullTextSearchDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FullTextSearchDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlogsDbContext _context;
        private readonly BlogService _blogService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(BlogsDbContext context, BlogService blogService, ILogger<HomeController> logger)
        {
            _context = context;
            _blogService = blogService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Search");
        }

        public IActionResult Search()
        {
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult TestSubmit(string searchTerm, string searchType = "basic")
        {
            // Log the submitted form data
            _logger.LogInformation("Form submitted in Test - Search Term: {SearchTerm}, Search Type: {SearchType}", searchTerm, searchType);
            
            // Store the form data in TempData to display it in the view
            TempData["SubmittedData"] = $"Search Term: {searchTerm}\nSearch Type: {searchType}\nSubmitted at: {DateTime.Now}";
            
            return RedirectToAction("Test");
        }

        public IActionResult SearchThai()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SearchResults(string searchTerm, string searchType = "basic")
        {
            // Log the submitted form data
            _logger.LogInformation("Form submitted - Search Term: {SearchTerm}, Search Type: {SearchType}", searchTerm, searchType);
            
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
