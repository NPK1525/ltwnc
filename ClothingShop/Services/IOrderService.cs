using System.Threading.Tasks;
using ClothingShop.Models;

namespace ClothingShop.Services
{
    public interface IOrderService
    {
        Task CancelOrderAsync(Order order, string cancelledBy, string cancelReason, int performedByUserId);

        /// <summary>
        /// Tạo đơn hàng mới: lưu Order + ghi voucher usage + trừ kho FIFO + ghi InventoryTransaction + tạo Notification.
        /// Toàn bộ thực hiện trong một DB transaction.
        /// </summary>
        Task<Order> CreateOrderAsync(CreateOrderRequest request);
    }

    /// <summary>
    /// DTO chứa tất cả dữ liệu cần để tạo đơn hàng, dùng chung cho COD và VNPay.
    /// </summary>
    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public required List<CartItem> CartItems { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Address { get; set; }
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Status { get; set; }

        /// <summary>Nội dung thông báo tuỳ chỉnh. Nếu null, dùng mặc định.</summary>
        public string? NotificationTitle { get; set; }
        public string? NotificationMessage { get; set; }
    }
}
