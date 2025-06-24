using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FullTextSearchDemo.Database
{
    internal sealed class BlogPostThaiConfiguration : IEntityTypeConfiguration<BlogPostThai>
    {
        public void Configure(EntityTypeBuilder<BlogPostThai> builder)
        {
            builder.HasKey(b => b.Slug);

            // Create a full-text search index on Title, Excerpt, and Content
            builder.HasIndex(b => new { b.Title, b.Excerpt, b.Content })
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("Thai");
        }
    }
}