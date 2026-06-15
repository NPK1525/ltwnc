// Models/ProductCategory.cs
namespace ClothingShop.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Fields từ FashionCategory (cho banner trang chủ)
        public string? BannerImageUrl { get; set; }
        public string? BannerImageUrlAvif { get; set; }
        public string? LinkUrl { get; set; }
        public bool IsFeatured { get; set; } = false; // Hiển thị trên trang chủ

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<Product>? Products { get; set; }
    }
}
