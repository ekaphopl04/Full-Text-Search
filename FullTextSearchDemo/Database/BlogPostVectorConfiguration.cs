using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FullTextSearchDemo.Database
{
    internal sealed class BlogPostVectorConfiguration : IEntityTypeConfiguration<BlogPostVector>
    {
        public void Configure(EntityTypeBuilder<BlogPostVector> builder)
        {
            builder.HasKey(b => b.Slug);

            // Create a full-text search index on Title, Excerpt, and Content
            builder
            .HasGeneratedTsVectorColumn(b => b.SearchVector, "english", b => new { b.Title, b.Excerpt, b.Content })
            .HasIndex(b => b.SearchVector)
            .HasMethod("GIN");
        }
    }

}           
