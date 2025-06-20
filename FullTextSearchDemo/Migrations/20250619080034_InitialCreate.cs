using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FullTextSearchDemo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
                
            // Create a function to perform the full-text search
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION search_products(search_term text)
                RETURNS SETOF ""Products""
                LANGUAGE sql
                AS $$
                    SELECT *
                    FROM ""Products""
                    WHERE to_tsvector('english', ""Name"" || ' ' || ""Description"" || ' ' || ""Category"") @@ to_tsquery('english', search_term)
                    ORDER BY ts_rank(to_tsvector('english', ""Name"" || ' ' || ""Description"" || ' ' || ""Category""), to_tsquery('english', search_term)) DESC;
                $$;
            ");

            // Create a trigger to automatically update the search vector when a product is inserted or updated
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION products_search_vector_update() RETURNS trigger AS $$
                BEGIN
                    NEW.""SearchVector"" = to_tsvector('english', NEW.""Name"" || ' ' || NEW.""Description"" || ' ' || NEW.""Category"");
                    RETURN NEW;
                END
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER products_search_vector_update
                BEFORE INSERT OR UPDATE ON ""Products""
                FOR EACH ROW
                EXECUTE FUNCTION products_search_vector_update();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the trigger and functions first
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS products_search_vector_update ON \"Products\";");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS products_search_vector_update();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS search_products(text);");
            
            // Then drop the table
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
