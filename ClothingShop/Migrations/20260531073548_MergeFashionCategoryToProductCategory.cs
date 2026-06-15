using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Migrations
{
    /// <inheritdoc />
    public partial class MergeFashionCategoryToProductCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrlAvif",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "BannerImageUrlAvif",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "ProductCategories");
        }
    }
}
