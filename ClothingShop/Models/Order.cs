using ClothingShop.Models.Enums;

namespace ClothingShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = OrderStatus.Pending.ToVietnamese(); // Pending, Processing, Completed, Cancelled

        // Thông tin giao hàng
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }

        // Thông tin thanh toán
        public string? PaymentMethod { get; set; } // "COD", "VNPay", "BankTransfer"
        public string? PaymentStatus { get; set; } // "Pending", "Paid", "Failed"
        public string? PaymentTransactionId { get; set; } // Mã giao dịch từ VNPay
        public DateTime? PaidAt { get; set; } // Thời điểm thanh toán thành công

        // Lý do hủy đơn (nếu có)
        public string? CancelReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; } // "Admin" hoặc "Customer"

        // Thông tin giảm giá voucher
        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }

        public List<OrderItem> Items { get; set; } = [];
    }
}