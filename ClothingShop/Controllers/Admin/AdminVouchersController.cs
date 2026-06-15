using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingShop.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Vouchers")]
    public class AdminVouchersController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // GET: /Admin/Vouchers
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, string? type)
        {
            var query = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.Now;
                query = status switch
                {
                    "active" => query.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now && (v.UsageLimit == null || _context.VoucherUsages.Count(u => u.VoucherId == v.Id) < v.UsageLimit)),
                    "inactive" => query.Where(v => !v.IsActive),
                    "expired" => query.Where(v => v.EndDate < now),
                    "exhausted" => query.Where(v => v.UsageLimit != null && _context.VoucherUsages.Count(u => u.VoucherId == v.Id) >= v.UsageLimit),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(type))
            {
                if (Enum.TryParse<VoucherDiscountType>(type, out var parsedType))
                {
                    query = query.Where(v => v.DiscountType == parsedType);
                }
            }

            var vouchers = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;

            return View("~/Views/Admin/Vouchers/Index.cshtml", vouchers);
        }

        // GET: /Admin/Vouchers/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/Vouchers/Create.cshtml", new Voucher
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                LimitPerUser = 1
            });
        }

        // POST: /Admin/Vouchers/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError("Code", "Mã voucher không được để trống!");
            }
            else
            {
                model.Code = model.Code.Trim().ToUpper();
                if (await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
                {
                    ModelState.AddModelError("Code", "Mã voucher này đã tồn tại trong hệ thống!");
                }
            }

            if (model.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Giá trị giảm phải lớn hơn 0!");
            }
            else if (model.DiscountType == VoucherDiscountType.Percentage && model.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm không được vượt quá 100%!");
            }

            if (model.MinOrderAmount < 0)
            {
                ModelState.AddModelError("MinOrderAmount", "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0!");
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
            }

            if (model.UsageLimit.HasValue && model.UsageLimit.Value < 1)
            {
                ModelState.AddModelError("UsageLimit", "Giới hạn sử dụng phải lớn hơn hoặc bằng 1!");
            }

            if (model.LimitPerUser < 1)
            {
                ModelState.AddModelError("LimitPerUser", "Giới hạn sử dụng của mỗi user phải lớn hơn hoặc bằng 1!");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.UsedCount = 0;

                _context.Vouchers.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm mới voucher {model.Code} thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/Vouchers/Create.cshtml", model);
        }

        // GET: /Admin/Vouchers/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }
            return View("~/Views/Admin/Vouchers/Edit.cshtml", voucher);
        }

        // POST: /Admin/Vouchers/Edit/{id}
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher model)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            // Kiểm tra nếu voucher đã được sử dụng từ DB thực tế
            var actualUsedCount = await _context.VoucherUsages.CountAsync(u => u.VoucherId == id);
            bool hasBeenUsed = actualUsedCount > 0;

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
            }

            if (model.UsageLimit.HasValue)
            {
                if (model.UsageLimit.Value < 1)
                {
                    ModelState.AddModelError("UsageLimit", "Giới hạn sử dụng phải lớn hơn hoặc bằng 1!");
                }
                else if (model.UsageLimit.Value < actualUsedCount)
                {
                    ModelState.AddModelError("UsageLimit", $"Giới hạn sử dụng mới không thể nhỏ hơn số lượt đã dùng thực tế ({actualUsedCount})!");
                }
            }

            // Nếu chưa sử dụng, cho phép validate các trường quan trọng khác
            if (!hasBeenUsed)
            {
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    ModelState.AddModelError("Code", "Mã voucher không được để trống!");
                }
                else
                {
                    model.Code = model.Code.Trim().ToUpper();
                    if (model.Code != voucher.Code && await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
                    {
                        ModelState.AddModelError("Code", "Mã voucher này đã tồn tại trong hệ thống!");
                    }
                }

                if (model.DiscountValue <= 0)
                {
                    ModelState.AddModelError("DiscountValue", "Giá trị giảm phải lớn hơn 0!");
                }
                else if (model.DiscountType == VoucherDiscountType.Percentage && model.DiscountValue > 100)
                {
                    ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm không được vượt quá 100%!");
                }

                if (model.MinOrderAmount < 0)
                {
                    ModelState.AddModelError("MinOrderAmount", "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0!");
                }

                if (model.LimitPerUser < 1)
                {
                    ModelState.AddModelError("LimitPerUser", "Giới hạn sử dụng của mỗi user phải lớn hơn hoặc bằng 1!");
                }
            }

            if (ModelState.IsValid)
            {
                // Cập nhật các trường luôn cho phép sửa
                voucher.EndDate = model.EndDate;
                voucher.UsageLimit = model.UsageLimit;
                voucher.IsActive = model.IsActive;
                voucher.Description = model.Description;

                // Nếu chưa dùng, cập nhật thêm các trường quan trọng
                if (!hasBeenUsed)
                {
                    voucher.Code = model.Code.Trim().ToUpper();
                    voucher.DiscountType = model.DiscountType;
                    voucher.DiscountValue = model.DiscountValue;
                    voucher.MaxDiscountAmount = model.MaxDiscountAmount;
                    voucher.MinOrderAmount = model.MinOrderAmount;
                    voucher.StartDate = model.StartDate;
                    voucher.LimitPerUser = model.LimitPerUser;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật voucher {voucher.Code} thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Trả về view và giữ lại trạng thái hasBeenUsed
            return View("~/Views/Admin/Vouchers/Edit.cshtml", model);
        }

        // GET: /Admin/Vouchers/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            // Lấy danh sách lịch sử sử dụng
            var usages = await _context.VoucherUsages
                .Include(u => u.User)
                .Include(u => u.Order)
                .Where(u => u.VoucherId == id)
                .OrderByDescending(u => u.UsedAt)
                .ToListAsync();

            ViewBag.Usages = usages;

            return View("~/Views/Admin/Vouchers/Details.cshtml", voucher);
        }

        // POST: /Admin/Vouchers/ToggleActive/{id}
        [HttpPost("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(voucher.IsActive ? "kích hoạt" : "hủy kích hoạt")} voucher {voucher.Code}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Vouchers/Delete/{id}
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            // Nếu voucher đã được dùng, không được xóa trực tiếp để tránh lỗi khóa ngoại, thay vào đó chỉ cho phép vô hiệu hóa
            var actualUsedCount = await _context.VoucherUsages.CountAsync(u => u.VoucherId == id);
            if (actualUsedCount > 0)
            {
                voucher.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Warning"] = $"Mã {voucher.Code} đã được sử dụng nên không thể xóa khỏi CSDL. Hệ thống đã chuyển trạng thái sang ngưng kích hoạt.";
            }
            else
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa vĩnh viễn voucher {voucher.Code}!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
