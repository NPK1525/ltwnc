using Microsoft.AspNetCore.Mvc;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using ClothingShop.Services;
using Microsoft.EntityFrameworkCore;
using static ClothingShop.Models.Enums.OrderStatusExtensions;
using ClothingShop.Models.Requests;
using ClothingShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace ClothingShop.Controllers
{
    [Authorize]
    public class OrdersController(ApplicationDbContext context, ICartService cartService, IConfiguration configuration, IVNPayService vnPayService, IOrderService orderService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ICartService _cartService = cartService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IVNPayService _vnPayService = vnPayService;
        private readonly IOrderService _orderService = orderService;

        // GET: Danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lấy danh sách sản phẩm đã được đánh giá cho mỗi đơn hàng của user này
            var userReviews = await _context.ProductReviews
                .Where(r => r.UserId == userId)
                .Select(r => new { r.OrderId, r.ProductId })
                .Distinct()
                .ToListAsync();

            var reviewedMap = userReviews
                .GroupBy(r => r.OrderId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ProductId).ToHashSet());

            var fullyReviewedOrderIds = new HashSet<int>();
            foreach (var order in orders)
            {
                if (order.Status == OrderStatus.Delivered.ToVietnamese())
                {
                    if (reviewedMap.TryGetValue(order.Id, out var reviewedProductIds))
                    {
                        var orderProductIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                        if (orderProductIds.All(pid => reviewedProductIds.Contains(pid)))
                        {
                            fullyReviewedOrderIds.Add(order.Id);
                        }
                    }
                }
            }

            ViewBag.FullyReviewedOrderIds = fullyReviewedOrderIds;

            return View(orders);
        }

        // GET: Trang Checkout
        public async Task<IActionResult> Checkout(string? selectedItems)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Orders/Checkout" });
            }

            // Nếu thanh toán giỏ hàng, xóa thông tin "Mua Ngay" cũ trong session
            if (!string.IsNullOrEmpty(selectedItems))
            {
                HttpContext.Session.Remove(Constants.SessionKeys.BuyNowProductId);
                HttpContext.Session.Remove(Constants.SessionKeys.BuyNowQuantity);
                HttpContext.Session.Remove(Constants.SessionKeys.BuyNowSize);
                HttpContext.Session.Remove(Constants.SessionKeys.BuyNowColor);
            }

            List<CartItem> cartItems;
            decimal totalPrice;

            // Kiểm tra xem có phải "Mua Ngay" không
            var buyNowProductId = HttpContext.Session.GetInt32(Constants.SessionKeys.BuyNowProductId);
            if (buyNowProductId.HasValue)
            {
                // Xử lý "Mua Ngay" - tạo giỏ hàng tạm thời
                var quantity = HttpContext.Session.GetInt32(Constants.SessionKeys.BuyNowQuantity) ?? 1;
                var size = HttpContext.Session.GetString(Constants.SessionKeys.BuyNowSize);
                var color = HttpContext.Session.GetString(Constants.SessionKeys.BuyNowColor);

                cartItems = await _cartService.GetBuyNowCartItemAsync(buyNowProductId.Value, quantity, size, color);
                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home");
                }
                totalPrice = cartItems.Sum(item => item.Price * item.Quantity);
                ViewBag.IsBuyNow = true;
            }
            else
            {
                // Xử lý giỏ hàng thông thường
                var allCartItems = _cartService.GetCartItems();
                if (allCartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index", "Cart");
                }

                // Nếu có selectedItems, chỉ lấy các sản phẩm đã chọn
                if (!string.IsNullOrEmpty(selectedItems))
                {
                    try
                    {
                        var selectedItemsList = System.Text.Json.JsonSerializer.Deserialize<List<SelectedCartItem>>(selectedItems);
                        if (selectedItemsList != null && selectedItemsList.Count > 0)
                        {
                            cartItems = [];
                            foreach (var selected in selectedItemsList)
                            {
                                var matchedItem = allCartItems.FirstOrDefault(item =>
                                    item.ProductId.ToString() == selected.ProductId &&
                                    item.Size == selected.Size &&
                                    item.Color == selected.Color
                                );

                                if (matchedItem != null)
                                {
                                    // Tạo bản sao và cập nhật số lượng
                                    var cartItem = new CartItem
                                    {
                                        ProductId = matchedItem.ProductId,
                                        Name = matchedItem.Name,
                                        Price = matchedItem.Price,
                                        ImageUrl = matchedItem.ImageUrl,
                                        Size = matchedItem.Size,
                                        Color = matchedItem.Color,
                                        Quantity = int.TryParse(selected.Quantity, out int qty) ? qty : matchedItem.Quantity
                                    };
                                    cartItems.Add(cartItem);
                                }
                            }

                            if (cartItems.Count == 0)
                            {
                                cartItems = allCartItems;
                            }
                        }
                        else
                        {
                            cartItems = allCartItems;
                        }
                    }
                    catch
                    {
                        cartItems = allCartItems;
                    }
                }
                else
                {
                    cartItems = allCartItems;
                }

                totalPrice = cartItems.Sum(item => item.Price * item.Quantity);
                ViewBag.IsBuyNow = false;
            }

            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = totalPrice;
            ViewBag.SelectedItems = selectedItems;

            // Lấy thông tin user
            var user = await _context.Users.FindAsync(userId);
            ViewBag.User = user;

            // Lấy thông tin thanh toán từ appsettings.json
            ViewBag.BankName = _configuration["PaymentInfo:BankName"] ?? "Vietcombank";
            ViewBag.BankAccountNumber = _configuration["PaymentInfo:BankAccountNumber"] ?? "1234567890";
            ViewBag.BankAccountName = _configuration["PaymentInfo:BankAccountName"] ?? "NGUYEN VAN A";
            ViewBag.MoMoPhone = _configuration["PaymentInfo:MoMoPhone"] ?? "0901234567";
            ViewBag.MoMoName = _configuration["PaymentInfo:MoMoName"] ?? "NGUYEN VAN A";

            return View();
        }

        // POST: Xử lý đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string fullName, string phoneNumber, string address, string? note, string? paymentMethod, string? couponCode, string? selectedItems)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            List<CartItem> cartItems;
            decimal totalPrice;

            // Kiểm tra xem có phải "Mua Ngay" không
            var buyNowProductId = HttpContext.Session.GetInt32(Constants.SessionKeys.BuyNowProductId);
            if (buyNowProductId.HasValue)
            {
                // Xử lý "Mua Ngay"
                var quantity = HttpContext.Session.GetInt32(Constants.SessionKeys.BuyNowQuantity) ?? 1;
                var size = HttpContext.Session.GetString(Constants.SessionKeys.BuyNowSize);
                var color = HttpContext.Session.GetString(Constants.SessionKeys.BuyNowColor);

                cartItems = await _cartService.GetBuyNowCartItemAsync(buyNowProductId.Value, quantity, size, color);
                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home");
                }
                var item = cartItems[0];
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.ProductId == buyNowProductId.Value && pv.Size == size && pv.Color == color);
                if (variant == null || variant.Quantity < quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.Name} (Size: {size}, Màu: {color}) không đủ số lượng tồn kho!";
                    return RedirectToAction("Checkout");
                }
                totalPrice = cartItems.Sum(i => i.Price * i.Quantity);
            }
            else
            {
                // Xử lý giỏ hàng thông thường
                var allCartItems = _cartService.GetCartItems();
                if (allCartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index", "Cart");
                }

                // Nếu có selectedItems, chỉ lấy các sản phẩm đã chọn
                if (!string.IsNullOrEmpty(selectedItems))
                {
                    try
                    {
                        var selectedItemsList = System.Text.Json.JsonSerializer.Deserialize<List<SelectedCartItem>>(selectedItems);
                        if (selectedItemsList != null && selectedItemsList.Count > 0)
                        {
                            cartItems = [];
                            foreach (var selected in selectedItemsList)
                            {
                                var matchedItem = allCartItems.FirstOrDefault(item =>
                                    item.ProductId.ToString() == selected.ProductId &&
                                    item.Size == selected.Size &&
                                    item.Color == selected.Color
                                );

                                if (matchedItem != null)
                                {
                                    var cartItem = new CartItem
                                    {
                                        ProductId = matchedItem.ProductId,
                                        Name = matchedItem.Name,
                                        Price = matchedItem.Price,
                                        ImageUrl = matchedItem.ImageUrl,
                                        Size = matchedItem.Size,
                                        Color = matchedItem.Color,
                                        Quantity = int.TryParse(selected.Quantity, out int qty) ? qty : matchedItem.Quantity
                                    };
                                    cartItems.Add(cartItem);
                                }
                            }

                            if (cartItems.Count == 0)
                            {
                                cartItems = allCartItems;
                            }
                        }
                        else
                        {
                            cartItems = allCartItems;
                        }
                    }
                    catch
                    {
                        cartItems = allCartItems;
                    }
                }
                else
                {
                    cartItems = allCartItems;
                }

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm hợp lệ trong giỏ hàng để thanh toán!";
                    return RedirectToAction("Checkout");
                }

                totalPrice = cartItems.Sum(item => item.Price * item.Quantity);

                // Kiểm tra số lượng tồn kho theo biến thể
                foreach (var item in cartItems)
                {
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(pv => pv.ProductId == item.ProductId && pv.Size == item.Size && pv.Color == item.Color);
                    if (variant == null || variant.Quantity < item.Quantity)
                    {
                        TempData["Error"] = $"Sản phẩm {item.Name} (Size: {item.Size}, Màu: {item.Color}) không đủ số lượng tồn kho!";
                        return RedirectToAction("Checkout");
                    }
                }
            }

            // Tính phí vận chuyển
            decimal shippingFee = totalPrice < Constants.Business.FreeShippingThreshold
                ? Constants.Business.StandardShippingFee
                : 0;

            // Xử lý mã giảm giá
            decimal discountAmount = 0;
            Voucher? appliedVoucher = null;
            if (!string.IsNullOrEmpty(couponCode))
            {
                couponCode = couponCode.Trim().ToUpper();
                var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == couponCode);
                var now = DateTime.Now;
                if (voucher != null &&
                    voucher.IsActive &&
                    now >= voucher.StartDate &&
                    now <= voucher.EndDate &&
                    totalPrice >= voucher.MinOrderAmount)
                {
                    // Tính số lần đã dùng thực tế từ DB thay vì dùng UsedCount cache
                    var actualUsedCount = await _context.VoucherUsages.CountAsync(u => u.VoucherId == voucher.Id);
                    if (!voucher.UsageLimit.HasValue || actualUsedCount < voucher.UsageLimit.Value)
                    {
                        // Kiểm tra giới hạn dùng của user
                        var userUsedCount = await _context.VoucherUsages
                            .CountAsync(u => u.VoucherId == voucher.Id && u.UserId == userId);
                        if (userUsedCount < voucher.LimitPerUser)
                        {
                            appliedVoucher = voucher;
                            if (voucher.DiscountType == VoucherDiscountType.Percentage)
                            {
                                discountAmount = totalPrice * (voucher.DiscountValue / 100);
                                if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
                                {
                                    discountAmount = voucher.MaxDiscountAmount.Value;
                                }
                            }
                            else
                            {
                                discountAmount = voucher.DiscountValue;
                            }

                            if (discountAmount > totalPrice)
                            {
                                discountAmount = totalPrice;
                            }
                        }
                    }
                }
            }

            decimal finalTotal = totalPrice + shippingFee - discountAmount;
            if (finalTotal < 0) finalTotal = 0;

            // Nếu chọn VNPay, tạo đơn hàng trong DB với trạng thái "Chờ thanh toán" trước khi redirect
            if (paymentMethod == "VNPay")
            {
                try
                {
                    var order = await _orderService.CreateOrderAsync(new CreateOrderRequest
                    {
                        UserId = userId,
                        CartItems = cartItems,
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        Address = address,
                        Note = note,
                        TotalAmount = finalTotal,
                        VoucherCode = appliedVoucher?.Code,
                        DiscountAmount = discountAmount,
                        Status = OrderStatus.WaitingPayment.ToVietnamese(),
                        NotificationTitle = "Đơn hàng đang chờ thanh toán",
                        NotificationMessage = "Đơn hàng của bạn đang chờ được thanh toán qua cổng VNPay."
                    });

                    // Xóa giỏ hàng / session mua ngay lập tức vì đơn hàng đã được khởi tạo
                    if (buyNowProductId.HasValue)
                    {
                        HttpContext.Session.Remove(Constants.SessionKeys.BuyNowProductId);
                        HttpContext.Session.Remove(Constants.SessionKeys.BuyNowQuantity);
                        HttpContext.Session.Remove(Constants.SessionKeys.BuyNowSize);
                        HttpContext.Session.Remove(Constants.SessionKeys.BuyNowColor);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(selectedItems))
                        {
                            foreach (var item in cartItems)
                            {
                                _cartService.RemoveFromCart(item.ProductId, item.Size, item.Color);
                            }
                        }
                        else
                        {
                            _cartService.ClearCart();
                        }
                    }

                    // Lấy IP address
                    string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    string orderInfo = $"Thanh toán đơn hàng #{order.Id:D6}";

                    // Tạo URL thanh toán VNPay bằng order.Id làm TxnRef
                    string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/VNPayReturn";
                    string paymentUrl = _vnPayService.CreatePaymentUrl(order.Id.ToString(), finalTotal, orderInfo, ipAddress, returnUrl);

                    return Redirect(paymentUrl);
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "Sản phẩm không đủ số lượng tồn kho do có người mua trước. Vui lòng kiểm tra lại!";
                    return RedirectToAction("Checkout");
                }
            }

            // Với COD hoặc Bank Transfer, tạo đơn hàng ngay
            try
            {
                var order = await _orderService.CreateOrderAsync(new CreateOrderRequest
                {
                    UserId = userId,
                    CartItems = cartItems,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    Note = note,
                    TotalAmount = finalTotal,
                    VoucherCode = appliedVoucher?.Code,
                    DiscountAmount = discountAmount
                });

                // Xóa session "Mua Ngay" hoặc giỏ hàng
                if (buyNowProductId.HasValue)
                {
                    HttpContext.Session.Remove(Constants.SessionKeys.BuyNowProductId);
                    HttpContext.Session.Remove(Constants.SessionKeys.BuyNowQuantity);
                    HttpContext.Session.Remove(Constants.SessionKeys.BuyNowSize);
                    HttpContext.Session.Remove(Constants.SessionKeys.BuyNowColor);
                }
                else
                {
                    if (!string.IsNullOrEmpty(selectedItems))
                    {
                        foreach (var item in cartItems)
                        {
                            _cartService.RemoveFromCart(item.ProductId, item.Size, item.Color);
                        }
                    }
                    else
                    {
                        _cartService.ClearCart();
                    }
                }

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderSuccess", new { id = order.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Sản phẩm không đủ số lượng tồn kho do có người mua trước. Vui lòng kiểm tra lại!";
                return RedirectToAction("Checkout");
            }
        }

        // GET: Trang đặt hàng thành công
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Kiểm tra xem user đã đánh giá từng sản phẩm chưa
            var reviewedProductIds = await _context.ProductReviews
                .Where(r => r.UserId == userId && r.OrderId == id)
                .Select(r => r.ProductId)
                .ToListAsync();

            ViewBag.ReviewedProductIds = reviewedProductIds;

            return View(order);
        }

        // POST: Hủy đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Chỉ cho phép hủy khi "Chờ xác nhận" hoặc "Chờ lấy hàng"
            if (order.Status != OrderStatus.Pending.ToVietnamese() && order.Status != OrderStatus.Confirmed.ToVietnamese())
            {
                TempData["Error"] = "Không thể hủy đơn hàng này. Chỉ có thể hủy khi đơn hàng đang chờ xác nhận hoặc chờ lấy hàng.";
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy đơn!";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                await _orderService.CancelOrderAsync(order, "Customer", cancelReason, userId);
                TempData["Success"] = "Đã hủy đơn hàng thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi hủy đơn hàng: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Xác nhận đã nhận hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Chỉ cho phép xác nhận khi đơn hàng đang "Chờ giao hàng"
            if (order.Status != OrderStatus.Shipping.ToVietnamese())
            {
                TempData["Error"] = "Không thể xác nhận đơn hàng này. Chỉ có thể xác nhận khi đơn hàng đang được giao.";
                return RedirectToAction("Details", new { id = orderId });
            }

            // Cập nhật trạng thái đơn hàng
            order.Status = OrderStatus.Delivered.ToVietnamese();

            await _context.SaveChangesAsync();

            // Tạo thông báo cho khách hàng
            var notification = new Notification
            {
                UserId = userId,
                Title = "Đơn hàng đã hoàn thành",
                Message = $"Cảm ơn bạn đã xác nhận nhận hàng cho đơn hàng #{order.Id:D6}. Hãy đánh giá sản phẩm để chia sẻ trải nghiệm của bạn!",
                Type = "success",
                OrderId = order.Id,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã xác nhận! Đơn hàng đã hoàn thành. Bạn có thể đánh giá sản phẩm ngay bây giờ.";
            return RedirectToAction("Details", new { id = orderId });
        }
    }
}
