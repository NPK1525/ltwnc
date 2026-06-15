using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ClothingShop.Controllers
{
    [Route("Vouchers")]
    public class VouchersController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // POST: /Vouchers/Apply
        [HttpPost("Apply")]
        public async Task<IActionResult> Apply(string code, decimal subtotal)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Json(new { isValid = false, message = "Vui lòng đăng nhập để sử dụng mã giảm giá!" });
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { isValid = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            code = code.Trim().ToUpper();
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null)
            {
                return Json(new { isValid = false, message = "Mã giảm giá không tồn tại!" });
            }

            // 1. Kiểm tra trạng thái hoạt động
            if (!voucher.IsActive)
            {
                return Json(new { isValid = false, message = "Mã giảm giá đã ngưng kích hoạt!" });
            }

            // 2. Kiểm tra thời hạn
            var now = DateTime.Now;
            if (now < voucher.StartDate)
            {
                return Json(new { isValid = false, message = "Mã giảm giá chưa đến thời gian có hiệu lực!" });
            }
            if (now > voucher.EndDate)
            {
                return Json(new { isValid = false, message = "Mã giảm giá đã hết hạn sử dụng!" });
            }

            // 3. Kiểm tra giới hạn sử dụng tổng thể trên hệ thống
            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
            {
                return Json(new { isValid = false, message = "Mã giảm giá đã hết lượt sử dụng trên hệ thống!" });
            }

            // 4. Kiểm tra giá trị đơn hàng tối thiểu
            if (subtotal < voucher.MinOrderAmount)
            {
                return Json(new { isValid = false, message = $"Giá trị đơn hàng tối thiểu phải từ {voucher.MinOrderAmount:N0}₫ để sử dụng mã này!" });
            }

            // 5. Kiểm tra giới hạn lượt dùng của từng user

            var userUsedCount = await _context.VoucherUsages
                .CountAsync(u => u.VoucherId == voucher.Id && u.UserId == userId);

            if (userUsedCount >= voucher.LimitPerUser)
            {
                return Json(new { isValid = false, message = $"Bạn đã đạt giới hạn sử dụng tối đa của mã giảm giá này ({voucher.LimitPerUser} lần)!" });
            }

            // 6. Tính toán giá trị giảm giá
            decimal discount = 0;
            string message = "";

            if (voucher.DiscountType == VoucherDiscountType.Percentage)
            {
                discount = subtotal * (voucher.DiscountValue / 100);
                if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                {
                    discount = voucher.MaxDiscountAmount.Value;
                }

                if (discount > subtotal)
                {
                    discount = subtotal;
                }

                message = $"Áp dụng thành công: Giảm {voucher.DiscountValue:G29}% (giảm tối đa {discount:N0}₫)";
            }
            else
            {
                discount = voucher.DiscountValue;
                if (discount > subtotal)
                {
                    discount = subtotal;
                }

                message = $"Áp dụng thành công: Giảm -{discount:N0}₫";
            }

            return Json(new
            {
                isValid = true,
                discount,
                message,
                code = voucher.Code
            });
        }

        // GET: /Vouchers/Available
        [HttpGet("Available")]
        public async Task<IActionResult> Available()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Json(Array.Empty<object>());
            }
            var now = DateTime.Now;

            // Lấy tất cả mã voucher đang còn hiệu lực
            var activeVouchers = await _context.Vouchers
                .Where(v => v.IsActive &&
                            v.StartDate <= now &&
                            v.EndDate >= now &&
                            (v.UsageLimit == null || v.UsedCount < v.UsageLimit))
                .OrderBy(v => v.MinOrderAmount)
                .ToListAsync();

            // Lọc ra các voucher chưa dùng quá giới hạn của user này
            var result = new System.Collections.Generic.List<object>();
            foreach (var voucher in activeVouchers)
            {
                var userUsedCount = await _context.VoucherUsages
                    .CountAsync(u => u.VoucherId == voucher.Id && u.UserId == userId);

                if (userUsedCount < voucher.LimitPerUser)
                {
                    result.Add(new
                    {
                        code = voucher.Code,
                        discountType = voucher.DiscountType,
                        discountValue = voucher.DiscountValue,
                        maxDiscountAmount = voucher.MaxDiscountAmount,
                        minOrderAmount = voucher.MinOrderAmount,
                        description = voucher.Description,
                        endDate = voucher.EndDate
                    });
                }
            }

            return Json(result);
        }
    }
}
