using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

namespace FullTextSearchDemo.Migrations
{
    /// <inheritdoc />
    public partial class Add_BlogPosts_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First create the table without the SearchVector column
            migrationBuilder.CreateTable(
                name: "BlogPostVectors",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Excerpt = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPostVectors", x => x.Slug);
                });

            // Add the SearchVector column as a computed column
            migrationBuilder.Sql(@"
                ALTER TABLE ""BlogPostVectors"" 
                ADD COLUMN ""SearchVector"" tsvector 
                GENERATED ALWAYS AS (to_tsvector('english', coalesce(""Title"",'') || ' ' || coalesce(""Excerpt"",'') || ' ' || coalesce(""Content"",''))) STORED;
            ");

            // Create the GIN index on the SearchVector column
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_BlogPostVectors_SearchVector"" ON ""BlogPostVectors"" USING GIN(""SearchVector"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the table and all related objects
            migrationBuilder.DropTable(
                name: "BlogPostVectors");
        }
    }
}
