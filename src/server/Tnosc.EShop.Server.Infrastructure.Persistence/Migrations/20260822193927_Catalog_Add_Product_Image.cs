using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Catalog_Add_Product_Image : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                schema: "catalog",
                table: "products",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                schema: "catalog",
                table: "products");
        }
    }
}
