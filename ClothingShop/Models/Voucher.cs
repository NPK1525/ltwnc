using System;
using System.ComponentModel.DataAnnotations;
using ClothingShop.Models.Enums;

namespace ClothingShop.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required]
        public VoucherDiscountType DiscountType { get; set; }

        [Required]
        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        [Required]
        public decimal MinOrderAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; }

        public int LimitPerUser { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
