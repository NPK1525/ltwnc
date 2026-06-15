using ClothingShop.Data;
using ClothingShop.Services;
using ClothingShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Common;
using Microsoft.AspNetCore.Authorization;
using ClothingShop.Models.Enums;

namespace ClothingShop.Controllers;

[Authorize]
public class PaymentController(ApplicationDbContext context, IVNPayService vnPayService, IOrderService orderService, ICartService cartService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly IVNPayService _vnPayService = vnPayService;
    private readonly IOrderService _orderService = orderService;
    private readonly ICartService _cartService = cartService;

    // GET: /Payment/VNPay
    [HttpGet]
    public async Task<IActionResult> VNPay(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng!";
            return RedirectToAction("Index", "Orders");
        }

        // Lấy IP address
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Tạo URL thanh toán
        string orderInfo = $"Thanh toán đơn hàng #{orderId:D6}";
        string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/VNPayReturn";
        string paymentUrl = _vnPayService.CreatePaymentUrl(orderId.ToString(), order.TotalAmount, orderInfo, ipAddress, returnUrl);

        return Redirect(paymentUrl);
    }

    // GET: /Payment/VNPayReturn
    [HttpGet]
    public async Task<IActionResult> VNPayReturn()
    {
        var queryParams = Request.Query;

        string vnpSecureHash = queryParams["vnp_SecureHash"]!;

        // Validate signature
        bool isValidSignature = _vnPayService.ValidateSignature(queryParams, vnpSecureHash);

        if (!isValidSignature)
        {
            TempData["Error"] = "Chữ ký không hợp lệ!";
            return RedirectToAction("Index", "Orders");
        }

        // Lấy thông tin giao dịch
        string vnpResponseCode = queryParams["vnp_ResponseCode"]!;
        string vnpTransactionNo = queryParams["vnp_TransactionNo"]!;
        string vnpTxnRef = queryParams["vnp_TxnRef"]!;

        // Parse orderId từ TxnRef (chính là order.Id dạng string)
        if (!int.TryParse(vnpTxnRef, out int orderId))
        {
            TempData["Error"] = "Mã giao dịch không hợp lệ!";
            return RedirectToAction("Index", "Orders");
        }

        // Tìm đơn hàng tương ứng
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng tương ứng!";
            return RedirectToAction("Index", "Orders");
        }

        // Nếu đơn hàng đã được cập nhật trạng thái trước đó (ví dụ từ IPN hoặc load lại trang)
        if (order.Status != OrderStatus.WaitingPayment.ToVietnamese())
        {
            if (order.Status == OrderStatus.Pending.ToVietnamese())
            {
                TempData["Success"] = $"Thanh toán thành công! Mã giao dịch: {vnpTransactionNo}";
                return RedirectToAction("OrderSuccess", "Orders", new { id = order.Id });
            }
            else
            {
                TempData["Error"] = "Đơn hàng đã được xử lý hoặc hủy trước đó!";
                return RedirectToAction("Index", "Orders");
            }
        }

        // Kiểm tra kết quả thanh toán
        if (vnpResponseCode == "00")
        {
            // Thanh toán thành công -> Cập nhật sang "Chờ xác nhận"
            try
            {
                order.Status = OrderStatus.Pending.ToVietnamese();

                // Tạo thông báo
                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = "Thanh toán thành công",
                    Message = $"Đơn hàng #{order.Id:D6} của bạn đã được thanh toán thành công qua VNPay. Mã giao dịch: {vnpTransactionNo}",
                    Type = "success",
                    OrderId = order.Id,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Thanh toán thành công! Mã giao dịch: {vnpTransactionNo}";
                return RedirectToAction("OrderSuccess", "Orders", new { id = order.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xác nhận đơn hàng: " + ex.Message;
                return RedirectToAction("Index", "Orders");
            }
        }
        else
        {
            // Thanh toán thất bại -> Hủy đơn hàng và hoàn kho
            string errorMessage = vnpResponseCode switch
            {
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => "Giao dịch không thành công!"
            };

            try
            {
                await _orderService.CancelOrderAsync(order, "System", $"Thanh toán VNPay thất bại: {errorMessage}", order.UserId);
            }
            catch (Exception)
            {
                // Ghi nhận lỗi hủy đơn hàng nếu có
            }

            TempData["Error"] = errorMessage;
            return RedirectToAction("Checkout", "Orders");
        }
    }
}
