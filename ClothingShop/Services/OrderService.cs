using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;

namespace ClothingShop.Services
{
    public class OrderService(ApplicationDbContext context) : IOrderService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Pre-load tất cả products cần dùng (tránh Find() đồng bộ trong Select)
                var productIds = request.CartItems.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                // 2. Tạo Order
                var order = new Order
                {
                    UserId = request.UserId,
                    OrderDate = DateTime.Now,
                    TotalAmount = request.TotalAmount,
                    Status = request.Status ?? OrderStatus.Pending.ToVietnamese(),
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    Note = request.Note,
                    VoucherCode = string.IsNullOrEmpty(request.VoucherCode) ? null : request.VoucherCode,
                    DiscountAmount = request.DiscountAmount,
                    Items = [.. request.CartItems.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Name,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Cost = products.TryGetValue(item.ProductId, out var p) ? p.Cost : null,
                        Size = item.Size,
                        Color = item.Color
                    })]
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Sinh Order.Id

                // 3. Ghi nhận sử dụng voucher
                if (!string.IsNullOrEmpty(request.VoucherCode))
                {
                    var voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(v => v.Code == request.VoucherCode);
                    if (voucher != null)
                    {
                        var usage = new VoucherUsage
                        {
                            VoucherId = voucher.Id,
                            UserId = request.UserId,
                            OrderId = order.Id,
                            UsedAt = DateTime.Now
                        };
                        _context.VoucherUsages.Add(usage);
                        voucher.UsedCount++; // Cập nhật UsedCount để đồng bộ với số lần dùng thực tế
                    }
                }

                // 4. Trừ kho FIFO + ghi InventoryTransaction
                foreach (var orderItem in order.Items)
                {
                    var product = products.GetValueOrDefault(orderItem.ProductId);
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(pv => pv.ProductId == orderItem.ProductId && pv.Size == orderItem.Size && pv.Color == orderItem.Color);

                    if (product != null && variant != null)
                    {
                        // Kiểm tra tồn kho đề phòng race condition
                        if (variant.Quantity < orderItem.Quantity)
                        {
                            throw new DbUpdateConcurrencyException("Sản phẩm không đủ số lượng tồn kho!");
                        }

                        // FIFO: trừ từ lô cũ nhất trước
                        int remainingToDeduct = orderItem.Quantity;
                        decimal totalCostForThisItem = 0;

                        var activeBatches = await _context.ProductVariantBatches
                            .Where(b => b.ProductVariantId == variant.Id && b.RemainingQuantity > 0)
                            .OrderBy(b => b.CreatedAt)
                            .ToListAsync();

                        foreach (var batch in activeBatches)
                        {
                            if (remainingToDeduct <= 0) break;

                            int takeQuantity = Math.Min(remainingToDeduct, batch.RemainingQuantity);
                            batch.RemainingQuantity -= takeQuantity;
                            totalCostForThisItem += takeQuantity * batch.Cost;
                            remainingToDeduct -= takeQuantity;
                        }

                        // Fallback nếu thiếu lô hàng (dữ liệu cũ)
                        if (remainingToDeduct > 0)
                        {
                            totalCostForThisItem += remainingToDeduct * (product.Cost ?? 0);
                        }

                        // Giá nhập bình quân FIFO
                        orderItem.Cost = totalCostForThisItem / orderItem.Quantity;

                        // Trừ tổng kho biến thể
                        variant.Quantity -= orderItem.Quantity;

                        // Ghi lịch sử giao dịch kho
                        var invTransaction = new InventoryTransaction
                        {
                            ProductId = product.Id,
                            ProductVariantId = variant.Id,
                            Type = "Xuất",
                            Quantity = orderItem.Quantity,
                            Reason = $"Xuất bán cho đơn hàng #{order.Id:D6} (Size: {orderItem.Size}, Màu: {orderItem.Color}) - FIFO Cost: {(orderItem.Cost.HasValue ? orderItem.Cost.Value.ToString("N0") : "0")}₫",
                            OrderId = order.Id,
                            CreatedBy = request.UserId,
                            CreatedAt = DateTime.Now
                        };
                        _context.InventoryTransactions.Add(invTransaction);
                    }
                }

                await _context.SaveChangesAsync();

                // 5. Tạo thông báo
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Title = request.NotificationTitle ?? "Đặt hàng thành công",
                    Message = request.NotificationMessage ?? $"Đơn hàng #{order.Id:D6} của bạn đã được đặt thành công. Chúng tôi sẽ xác nhận đơn hàng sớm nhất.",
                    Type = "success",
                    OrderId = order.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelOrderAsync(Order order, string cancelledBy, string cancelReason, int performedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.Status = OrderStatus.Cancelled.ToVietnamese();
                order.CancelReason = cancelReason;
                order.CancelledAt = DateTime.Now;
                order.CancelledBy = cancelledBy;

                // 1. Hoàn lại lượt dùng voucher — xóa bản ghi VoucherUsage, UsedCount tự sync
                if (!string.IsNullOrEmpty(order.VoucherCode))
                {
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == order.VoucherCode);
                    if (voucher != null)
                    {
                        var usage = await _context.VoucherUsages
                            .FirstOrDefaultAsync(u => u.VoucherId == voucher.Id && u.OrderId == order.Id);
                        if (usage != null)
                        {
                            _context.VoucherUsages.Remove(usage);
                            if (voucher.UsedCount > 0)
                            {
                                voucher.UsedCount--; // Hoàn lại UsedCount khi đơn hàng bị hủy
                            }
                        }
                    }
                }

                // 2. Hoàn lại số lượng tồn kho và ghi lịch sử giao dịch kho
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(pv => pv.ProductId == item.ProductId && pv.Size == item.Size && pv.Color == item.Color);

                    if (product != null)
                    {
                        if (variant != null)
                        {
                            variant.Quantity += item.Quantity;

                            // Hoàn trả số lượng vào lô hàng mới nhất của biến thể này để tiếp tục bán
                            var latestBatch = await _context.ProductVariantBatches
                                .Where(b => b.ProductVariantId == variant.Id)
                                .OrderByDescending(b => b.CreatedAt)
                                .FirstOrDefaultAsync();

                            if (latestBatch != null)
                            {
                                latestBatch.RemainingQuantity += item.Quantity;
                            }
                            else
                            {
                                // Tạo lô hoàn trả mặc định nếu không tìm thấy lô hàng
                                var refundBatch = new ProductVariantBatch
                                {
                                    ProductVariantId = variant.Id,
                                    BatchNumber = $"REFUND-#{order.Id:D6}",
                                    Cost = item.Cost ?? product.Cost ?? 0,
                                    ImportQuantity = item.Quantity,
                                    RemainingQuantity = item.Quantity,
                                    CreatedAt = DateTime.Now
                                };
                                _context.ProductVariantBatches.Add(refundBatch);
                            }
                        }

                        string txReason = cancelledBy == "Customer"
                            ? $"Nhập hoàn trả từ đơn hàng hủy #{order.Id:D6} (Size: {item.Size}, Màu: {item.Color})"
                            : $"Nhập hoàn trả (Admin hủy đơn hàng #{order.Id:D6}). Lý do: {cancelReason} (Size: {item.Size}, Màu: {item.Color})";

                        var invTransaction = new InventoryTransaction
                        {
                            ProductId = product.Id,
                            ProductVariantId = variant?.Id,
                            Type = "Nhập",
                            Quantity = item.Quantity,
                            Reason = txReason,
                            OrderId = order.Id,
                            CreatedBy = performedByUserId,
                            CreatedAt = DateTime.Now
                        };
                        _context.InventoryTransactions.Add(invTransaction);
                    }
                }

                // 3. Tạo thông báo cho khách hàng
                string notiTitle = cancelledBy == "Customer" ? "Đơn hàng đã được hủy" : "Đơn hàng đã bị hủy";
                string notiMessage = cancelledBy == "Customer"
                    ? $"Đơn hàng #{order.Id:D6} của bạn đã được hủy thành công."
                    : $"Đơn hàng #{order.Id:D6} đã bị hủy bởi quản trị viên. Lý do: {cancelReason}";
                string notiType = cancelledBy == "Customer" ? "warning" : "danger";

                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = notiTitle,
                    Message = notiMessage,
                    Type = notiType,
                    OrderId = order.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
