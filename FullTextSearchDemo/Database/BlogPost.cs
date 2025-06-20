using System;
using System.ComponentModel.DataAnnotations;

namespace FullTextSearchDemo.Database
{
    public class BlogPost
    {
        public string Slug { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Excerpt { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

    }
}
