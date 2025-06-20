using Microsoft.EntityFrameworkCore.Migrations;

namespace Blogs.Api.Migrations
{
    /// <inheritdoc />
    public partial class Add_BlogPosts_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Title_Excerpt_Content",
                table: "BlogPosts",
                columns: new[] { "Title", "Excerpt", "Content" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "English");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Title_Excerpt_Content",
                table: "BlogPosts");
        }
    }
}
