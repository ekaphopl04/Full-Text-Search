using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FullTextSearchDemo.Database
{
    internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
    {
        public void Configure(EntityTypeBuilder<BlogPost> builder)
        {
            builder.HasKey(b => b.Slug);

            // Create a full-text search index on Title, Excerpt, and Content
            builder.HasIndex(b => new { b.Title, b.Excerpt, b.Content })
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("English");
        }
    }
}
