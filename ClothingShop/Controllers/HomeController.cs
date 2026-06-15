using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using ClothingShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ClothingShop.Controllers
{
    public class HomeController(ApplicationDbContext context, ICartService cartService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ICartService _cartService = cartService;

        // GET: /
        public async Task<IActionResult> Index()
        {
            // Lấy sản phẩm bán chạy (dựa trên số lượng đã bán trong OrderItems)
            var bestSellingProducts = await _context.OrderItems
                .Where(oi => oi.Order.Status == OrderStatus.Delivered.ToVietnamese())
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(12)
                .Join(_context.Products.Where(p => p.ProductVariants.Any(pv => pv.Quantity > 0)),
                      x => x.ProductId,
                      p => p.Id,
                      (x, p) => p)
                .ToListAsync();

            // Nếu không có sản phẩm bán chạy, lấy sản phẩm mới nhất làm danh sách bán chạy mặc định
            if (bestSellingProducts.Count == 0)
            {
                bestSellingProducts = await _context.Products
                    .Where(p => p.ProductVariants.Any(pv => pv.Quantity > 0))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(12)
                    .ToListAsync();
            }

            ViewBag.BestSellingProducts = bestSellingProducts;

            // Lấy sản phẩm mới nhất (New Arrivals)
            var newArrivals = await _context.Products
                .Where(p => p.ProductVariants.Any(pv => pv.Quantity > 0))
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();
            ViewBag.NewArrivals = newArrivals;

            // Lấy danh mục nổi bật (Featured Categories)
            var featuredCategories = await _context.ProductCategories
                .Where(c => c.IsActive && c.IsFeatured)
                .OrderBy(c => c.DisplayOrder)
                .Take(4)
                .ToListAsync();

            if (featuredCategories.Count == 0)
            {
                featuredCategories = await _context.ProductCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Take(4)
                    .ToListAsync();
            }
            ViewBag.FeaturedCategories = featuredCategories;

            // CẬP NHẬT BADGE GIỎ HÀNG
            ViewBag.CartCount = _cartService.GetTotalItems();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}