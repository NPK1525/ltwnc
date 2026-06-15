using System.ComponentModel.DataAnnotations;

namespace ClothingShop.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Size { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = null!;

        [Required]
        public int Quantity { get; set; } = 0;

        [Required]
        public decimal Price { get; set; } = 0; // Giá bán riêng của biến thể này

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Navigation properties
        public Product? Product { get; set; }
    }
}
