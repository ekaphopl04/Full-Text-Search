using System;
using System.ComponentModel.DataAnnotations;

namespace FullTextSearchDemo.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        
        [Required]
        public string Slug { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string Excerpt { get; set; } = string.Empty;
        
        public string Content { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
