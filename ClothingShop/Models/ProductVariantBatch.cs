using System.ComponentModel.DataAnnotations;

namespace ClothingShop.Models
{
    public class ProductVariantBatch
    {
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        [MaxLength(50)]
        public string BatchNumber { get; set; } = null!;

        [Required]
        public decimal Cost { get; set; } // Giá nhập của lô này

        [Required]
        public int ImportQuantity { get; set; } // Số lượng nhập ban đầu

        [Required]
        public int RemainingQuantity { get; set; } // Số lượng tồn thực tế của lô này

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ProductVariant? ProductVariant { get; set; }
    }
}
