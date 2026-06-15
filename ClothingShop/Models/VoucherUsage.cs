using System;

namespace ClothingShop.Models
{
    public class VoucherUsage
    {
        public int Id { get; set; }

        public int VoucherId { get; set; }
        public Voucher Voucher { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public DateTime UsedAt { get; set; } = DateTime.Now;
    }
}
