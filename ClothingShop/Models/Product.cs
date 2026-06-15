// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingShop.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? Cost { get; set; } // Giá nhập (tùy chọn)
        public string? ImageUrl { get; set; }

        // ẢNH AVIF (tối ưu hiệu suất)
        public string? ImageUrlAvif { get; set; }

        // ẢNH PHỤ (lưu dạng JSON string)
        public string? AdditionalImages { get; set; }

        // ẢNH PHỤ AVIF (lưu dạng JSON string)
        public string? AdditionalImagesAvif { get; set; }

        // THÊM CÁC TRƯỜNG LỌC
        public string Category { get; set; } = "Áo"; // Áo, Quần, Giày, Phụ kiện
        public string Gender { get; set; } = "Nam"; // Nam, Nữ, Unisex

        // ✅ FOREIGN KEY đến ProductCategories
        public int? CategoryId { get; set; }
        public ProductCategory? ProductCategory { get; set; }

        // SIZE VÀ MÀU (lưu dạng JSON string - có thể chọn nhiều) - Dùng [NotMapped] để tránh lưu cột dư thừa
        [NotMapped]
        private string? _size;
        [NotMapped]
        public string Size
        {
            get
            {
                if (_size != null) return _size;
                if (ProductVariants != null && ProductVariants.Count > 0)
                {
                    var distinctSizes = ProductVariants.Select(pv => pv.Size).Distinct().ToList();
                    return System.Text.Json.JsonSerializer.Serialize(distinctSizes);
                }
                return "[]";
            }
            set
            {
                _size = value;
            }
        }

        [NotMapped]
        private string? _color;
        [NotMapped]
        public string Color
        {
            get
            {
                if (_color != null) return _color;
                if (ProductVariants != null && ProductVariants.Count > 0)
                {
                    var distinctColors = ProductVariants.Select(pv => pv.Color).Distinct().ToList();
                    return System.Text.Json.JsonSerializer.Serialize(distinctColors);
                }
                return "[]";
            }
            set
            {
                _color = value;
            }
        }

        [NotMapped]
        private int? _quantity;
        [NotMapped]
        public int Quantity
        {
            get
            {
                if (_quantity.HasValue) return _quantity.Value;
                if (ProductVariants != null && ProductVariants.Count > 0)
                {
                    return ProductVariants.Sum(pv => pv.Quantity);
                }
                return 0;
            }
            set
            {
                _quantity = value;
            }
        }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false; // Soft delete
        public DateTime? DeletedAt { get; set; } // Thời gian xóa
        public bool IsNew => CreatedAt > DateTime.Now.AddDays(-7);

        // Navigation properties
        public List<ProductVariant> ProductVariants { get; set; } = new();
    }
}