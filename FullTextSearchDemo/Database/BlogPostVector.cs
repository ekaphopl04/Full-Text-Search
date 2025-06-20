using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace FullTextSearchDemo.Database
{
    public class BlogPostVector
    {
        public string Slug { get; set; } = null!;
        
        public string Title { get; set; } = null!;
        
        public string Excerpt { get; set; } = null!;
        
        public string Content { get; set; } = null!;
        
        public string Date { get; set; } = null!;
        
        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }
}
