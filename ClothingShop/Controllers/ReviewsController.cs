using Microsoft.AspNetCore.Mvc;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ClothingShop.Controllers
{
    public partial class ReviewsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        [GeneratedRegex("<.*?>")]
        private static partial Regex HtmlTagRegex();

        // POST: Thêm đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int orderId, int rating, string? comment)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đánh giá";
                return RedirectToAction("Login", "Account");
            }

            // ✅ VALIDATION
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Đánh giá phải từ 1-5 sao";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            if (!string.IsNullOrEmpty(comment) && comment.Length > 1000)
            {
                TempData["Error"] = "Nhận xét không được quá 1000 ký tự";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // ✅ XSS PROTECTION - Remove HTML tags
            if (!string.IsNullOrEmpty(comment))
            {
                comment = HtmlTagRegex().Replace(comment, string.Empty);
                comment = comment.Trim();
            }

            // Kiểm tra xem đơn hàng cụ thể (orderId) có tồn tại, thuộc về user này, đã giao và chứa sản phẩm này hay không
            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == productId &&
                               oi.OrderId == orderId &&
                               oi.Order.UserId == userId &&
                               oi.Order.Status == OrderStatus.Delivered.ToVietnamese());

            if (!hasPurchased)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc bạn chưa mua sản phẩm này.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // Kiểm tra xem user đã đánh giá sản phẩm này cho đơn hàng này chưa (tránh đánh giá trùng lặp)
            var hasReviewed = await _context.ProductReviews
                .AnyAsync(r => r.ProductId == productId &&
                               r.OrderId == orderId &&
                               r.UserId == userId);

            if (hasReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                OrderId = orderId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
            return Redirect($"/Products/Details/{productId}#reviews");
        }

        // GET: Lấy đánh giá của sản phẩm
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Json(reviews);
        }
    }
}
