using ClothingShop.Data;
using ClothingShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Categories")]
    public class AdminCategoriesController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // ==================== PRODUCT CATEGORIES ====================

        // GET: /Admin/Categories/Product
        [HttpGet("Product")]
        public async Task<IActionResult> Product()
        {
            var categories = await _context.ProductCategories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View("~/Views/Admin/ProductCategories.cshtml", categories);
        }

        [HttpPost("Product/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductCategory model)
        {
            if (model.DisplayOrder == 0)
            {
                var maxOrder = await _context.ProductCategories.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
                model.DisplayOrder = maxOrder + 1;
            }
            else
            {
                var exists = await _context.ProductCategories.AnyAsync(c => c.DisplayOrder == model.DisplayOrder);
                if (exists)
                {
                    TempData["Error"] = $"Thứ tự {model.DisplayOrder} đã tồn tại! Vui lòng chọn số khác.";
                    return RedirectToAction(nameof(Product));
                }
            }

            if (ModelState.IsValid)
            {
                _context.ProductCategories.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm danh mục thành công!";
            }
            return RedirectToAction(nameof(Product));
        }

        [HttpPost("Product/Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(ProductCategory model)
        {
            var category = await _context.ProductCategories.FindAsync(model.Id);
            if (category != null)
            {
                if (category.DisplayOrder != model.DisplayOrder)
                {
                    var exists = await _context.ProductCategories
                        .AnyAsync(c => c.DisplayOrder == model.DisplayOrder && c.Id != model.Id);
                    if (exists)
                    {
                        TempData["Error"] = $"Thứ tự {model.DisplayOrder} đã tồn tại! Vui lòng chọn số khác.";
                        return RedirectToAction(nameof(Product));
                    }
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.DisplayOrder = model.DisplayOrder;
                category.IsActive = model.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật danh mục thành công!";
            }
            return RedirectToAction(nameof(Product));
        }

        [HttpPost("Product/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category != null)
            {
                var hasProducts = await _context.Products.AnyAsync(p => p.Category == category.Name);
                if (hasProducts)
                {
                    TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm!";
                }
                else
                {
                    _context.ProductCategories.Remove(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa danh mục thành công!";
                }
            }
            return RedirectToAction(nameof(Product));
        }
    }
}
