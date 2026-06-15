namespace ClothingShop.Models.Enums
{
    public enum OrderStatus
    {
        Pending,        // Chờ xác nhận
        WaitingPayment, // Chờ thanh toán (VNPay)
        Confirmed,      // Chờ lấy hàng
        Shipping,       // Chờ giao hàng
        Delivered,      // Đã giao
        Cancelled       // Đã hủy
    }

    public static class OrderStatusExtensions
    {
        public static string ToVietnamese(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xác nhận",
                OrderStatus.WaitingPayment => "Chờ thanh toán",
                OrderStatus.Confirmed => "Chờ lấy hàng",
                OrderStatus.Shipping => "Chờ giao hàng",
                OrderStatus.Delivered => "Đã giao",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        public static OrderStatus FromVietnamese(string status)
        {
            return status switch
            {
                "Chờ xác nhận" => OrderStatus.Pending,
                "Chờ thanh toán" => OrderStatus.WaitingPayment,
                "Chờ lấy hàng" => OrderStatus.Confirmed,
                "Chờ giao hàng" => OrderStatus.Shipping,
                "Đã giao" => OrderStatus.Delivered,
                "Đã hủy" => OrderStatus.Cancelled,
                _ => OrderStatus.Pending
            };
        }

        public static string GetBadgeClass(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "badge-warning",
                OrderStatus.WaitingPayment => "badge-info",
                OrderStatus.Confirmed => "badge-primary",
                OrderStatus.Shipping => "badge-info",
                OrderStatus.Delivered => "badge-success",
                OrderStatus.Cancelled => "badge-danger",
                _ => "badge-secondary"
            };
        }
    }
}
