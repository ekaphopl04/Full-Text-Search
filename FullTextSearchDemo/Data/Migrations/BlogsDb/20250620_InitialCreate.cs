using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace FullTextSearchDemo.Data.Migrations.BlogsDb
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Excerpt = table.Column<string>(nullable: true),
                    Content = table.Column<string>(nullable: true),
                    Date = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                });

            // Create full-text search index using PostgreSQL's GIN and tsvector
            migrationBuilder.Sql(@"
                CREATE INDEX IX_BlogPosts_Title_Excerpt_Content ON ""BlogPosts"" USING GIN (
                    to_tsvector('english', ""Title"" || ' ' || ""Excerpt"" || ' ' || ""Content"")
                );
            ");
            
            // Alternative approach using EF Core annotations
            // migrationBuilder.CreateIndex(
            //     name: "IX_BlogPosts_Title_Excerpt_Content",
            //     table: "BlogPosts",
            //     columns: new[] { "Title", "Excerpt", "Content" })
            //     .Annotation("Npgsql:IndexMethod", "GIN")
            //     .Annotation("Npgsql:TsVectorConfig", "english");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts");
        }
    }
}
