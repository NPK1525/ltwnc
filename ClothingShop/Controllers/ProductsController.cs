// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.Controllers
{
    public class ProductsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index(
            string category = "",
            string gender = "",
            string size = "",
            string color = "",
            string search = "",
            string sort = "newest",
            int page = 1)
        {
            const int pageSize = 12; // 12 sản phẩm mỗi trang

            // Load danh mục động cho bộ lọc
            ViewBag.Categories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => c.Name)
                .ToListAsync();

            var products = _context.Products.Where(p => p.ProductVariants.Any(pv => pv.Quantity > 0)).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.Name.Contains(search));

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category);
            if (!string.IsNullOrEmpty(gender))
                products = products.Where(p => p.Gender == gender || p.Gender == "Unisex");
            if (!string.IsNullOrEmpty(size) && !string.IsNullOrEmpty(color))
            {
                products = products.Where(p => p.ProductVariants.Any(pv => pv.Size == size && pv.Color == color && pv.Quantity > 0));
            }
            else
            {
                if (!string.IsNullOrEmpty(size))
                    products = products.Where(p => p.ProductVariants.Any(pv => pv.Size == size && pv.Quantity > 0));
                if (!string.IsNullOrEmpty(color))
                    products = products.Where(p => p.ProductVariants.Any(pv => pv.Color == color && pv.Quantity > 0));
            }

            products = sort switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "name" => products.OrderBy(p => p.Name),
                _ => products.OrderByDescending(p => p.CreatedAt)
            };

            // Phân trang
            var totalItems = await products.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            var paginatedProducts = await products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy danh sách Product IDs trên trang hiện tại để tối ưu truy vấn rating
            var productIds = paginatedProducts.Select(p => p.Id).ToList();

            // Lấy rating chỉ cho các sản phẩm hiển thị ở trang hiện tại
            var productRatings = await _context.ProductReviews
                .Where(r => productIds.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    TotalReviews = g.Count()
                })
                .ToListAsync();

            // Luôn khởi tạo ViewBag.ProductRatings (ngay cả khi rỗng)
            ViewBag.ProductRatings = productRatings.Count > 0
                ? productRatings.ToDictionary(x => x.ProductId, x => (dynamic)new { x.AverageRating, x.TotalReviews })
                : [];

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Gender = gender;
            ViewBag.Size = size;
            ViewBag.Color = color;
            ViewBag.Sort = sort;

            return View(paginatedProducts);
        }

        // CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> Details(int id, int? orderId = null)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Đồng bộ nếu chưa có biến thể
            if (product.ProductVariants == null || product.ProductVariants.Count == 0)
            {
                await ClothingShop.Common.ProductVariantHelper.SyncVariantsAsync(_context, product);
                product = await _context.Products
                    .Include(p => p.ProductVariants)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }

            // Tự động ghi nhận lượt xem
            await TrackProductView(id);

            // Lấy đánh giá của sản phẩm
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            ViewBag.TotalReviews = reviews.Count;

            // Kiểm tra xem user đã mua sản phẩm này chưa (để hiển thị form đánh giá)
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
            {
                if (orderId.HasValue)
                {
                    // 1. Kiểm tra đơn hàng có tồn tại, thuộc user, đã giao và chứa sản phẩm này không
                    var orderValid = await _context.OrderItems
                        .AnyAsync(oi => oi.ProductId == id &&
                                       oi.OrderId == orderId.Value &&
                                       oi.Order.UserId == userId &&
                                       oi.Order.Status == OrderStatus.Delivered.ToVietnamese());

                    if (orderValid)
                    {
                        // 2. Kiểm tra xem sản phẩm trong đơn hàng này đã được đánh giá chưa
                        var alreadyReviewed = await _context.ProductReviews
                            .AnyAsync(r => r.ProductId == id &&
                                           r.OrderId == orderId.Value &&
                                           r.UserId == userId);

                        if (!alreadyReviewed)
                        {
                            ViewBag.CanReview = true;
                            ViewBag.OrderId = orderId.Value;
                        }
                    }
                }
            }

            // Lấy sản phẩm liên quan (cùng danh mục hoặc cùng giới tính, loại trừ sản phẩm hiện tại)
            var relatedProducts = await _context.Products
                .Where(p => p.Id != id && p.ProductVariants.Any(pv => pv.Quantity > 0) &&
                       (p.Category == product!.Category || p.Gender == product!.Gender))
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // Hàm helper để track lượt xem sản phẩm
        private async Task TrackProductView(int productId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                var sessionId = HttpContext.Session.Id;

                int? userId = null;
                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Kiểm tra xem đã xem trong vòng 5 phút chưa
                var recentView = await _context.ProductViews
                    .Where(pv => pv.ProductId == productId &&
                                pv.ViewedAt >= DateTime.Now.AddMinutes(-5) &&
                                (userId.HasValue ? pv.UserId == userId : pv.SessionId == sessionId))
                    .FirstOrDefaultAsync();

                if (recentView == null)
                {
                    var productView = new ProductView
                    {
                        UserId = userId,
                        ProductId = productId,
                        SessionId = userId.HasValue ? null : sessionId,
                        ViewedAt = DateTime.Now
                    };

                    _context.ProductViews.Add(productView);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    recentView.ViewedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Không làm gì nếu có lỗi (không ảnh hưởng đến việc xem sản phẩm)
            }
        }
    }
}