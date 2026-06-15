using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Data;
using ClothingShop.Models;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Common;

namespace ClothingShop.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Inventory")]
    public class InventoryController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // GET: /Admin/Inventory/GetVariants
        [HttpGet("GetVariants")]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            var hasVariants = await _context.ProductVariants.AnyAsync(pv => pv.ProductId == productId);
            if (!hasVariants)
            {
                await ClothingShop.Common.ProductVariantHelper.SyncVariantsAsync(_context, product);
            }

            var sizes = ClothingShop.Common.ProductVariantHelper.GetActiveSizes(product);
            var colors = ClothingShop.Common.ProductVariantHelper.GetActiveColors(product);
            var validKeys = new HashSet<(string size, string color)>(
                sizes.SelectMany(s => colors.Select(c => (s, c)))
            );

            var dbVariants = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .OrderBy(pv => pv.Size)
                .ThenBy(pv => pv.Color)
                .ToListAsync();

            var activeVariants = dbVariants
                .Where(pv => validKeys.Contains((pv.Size, pv.Color)))
                .Select(pv => new { pv.Id, pv.Size, pv.Color, pv.Quantity, pv.Price })
                .ToList();

            return Json(activeVariants);
        }

        // GET: /Admin/Inventory - Danh sách giao dịch kho
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? type, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.InventoryTransactions
                .Include(it => it.Product)
                .Include(it => it.ProductVariant)
                .Include(it => it.Creator)
                .AsQueryable();

            // Lọc theo loại
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(it => it.Type == type);
                ViewBag.Type = type;
            }

            // Lọc theo ngày
            if (startDate.HasValue)
            {
                query = query.Where(it => it.CreatedAt >= startDate.Value);
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            }

            if (endDate.HasValue)
            {
                query = query.Where(it => it.CreatedAt <= endDate.Value);
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            }

            query = query.OrderByDescending(it => it.CreatedAt);

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê tổng quan
            ViewBag.TotalImport = await _context.InventoryTransactions
                .Where(it => it.Type == "Nhập")
                .SumAsync(it => it.Quantity);

            ViewBag.TotalExport = await _context.InventoryTransactions
                .Where(it => it.Type == "Xuất")
                .SumAsync(it => it.Quantity);

            return View(transactions);
        }

        // GET: /Admin/Inventory/ImportStock - Form nhập kho
        [HttpGet("ImportStock")]
        public async Task<IActionResult> ImportStock(int? productId)
        {
            ViewBag.Products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Nếu có productId, tự động chọn sản phẩm đó
            if (productId.HasValue)
            {
                ViewBag.SelectedProductId = productId.Value;
                var product = await _context.Products.FindAsync(productId.Value);
                ViewBag.SelectedProduct = product;
            }

            return View();
        }

        // POST: /Admin/Inventory/ImportStock - Xử lý nhập kho
        [HttpPost("ImportStock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStock(int productId, int variantId, int quantity, string? supplier, decimal? cost, decimal sellingPrice, string? reason, string batchNumber)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0!";
                return RedirectToAction(nameof(ImportStock));
            }

            if (sellingPrice <= 0)
            {
                TempData["Error"] = "Giá bán phải lớn hơn 0!";
                return RedirectToAction(nameof(ImportStock));
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(ImportStock));
            }

            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null || variant.ProductId != productId)
            {
                TempData["Error"] = "Không tìm thấy biến thể hợp lệ!";
                return RedirectToAction(nameof(ImportStock));
            }

            // Tạo mã lô hàng mặc định nếu để trống
            var finalBatchNumber = string.IsNullOrWhiteSpace(batchNumber)
                ? $"BATCH-{DateTime.Now:yyyyMMdd-HHmmss}"
                : batchNumber.Trim();

            // Tạo giao dịch nhập kho
            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                ProductVariantId = variantId,
                Type = "Nhập",
                Quantity = quantity,
                Supplier = supplier,
                Cost = cost,
                Reason = reason ?? $"Nhập kho biến thể Size {variant.Size} - Màu {variant.Color} (Lô: {finalBatchNumber})",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.InventoryTransactions.Add(transaction);

            // TẠO LÔ HÀNG TỒN KHO MỚI (BATCH)
            var variantBatch = new ProductVariantBatch
            {
                ProductVariantId = variantId,
                BatchNumber = finalBatchNumber,
                Cost = cost ?? 0,
                ImportQuantity = quantity,
                RemainingQuantity = quantity,
                CreatedAt = DateTime.Now
            };

            _context.ProductVariantBatches.Add(variantBatch);

            // Cập nhật số lượng tồn kho biến thể và giá bán riêng của biến thể
            variant.Quantity += quantity;
            variant.Price = sellingPrice;

            // Cập nhật giá bán tối thiểu cho Product dựa trên tất cả biến thể có giá > 0
            var allVariants = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .ToListAsync();

            // Gán trực tiếp giá trị của biến thể hiện tại vừa cập nhật
            var curVar = allVariants.FirstOrDefault(v => v.Id == variantId);
            if (curVar != null)
            {
                curVar.Price = sellingPrice;
            }

            var activePrices = allVariants.Where(v => v.Price > 0).Select(v => v.Price).ToList();
            var minPrice = activePrices.Count > 0 ? activePrices.Min() : sellingPrice;
            product.Price = minPrice;

            // Cập nhật giá nhập tổng thể (Cost) của sản phẩm để tham khảo nhanh
            if (cost.HasValue)
            {
                product.Cost = cost.Value;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã nhập lô '{finalBatchNumber}' với {quantity} sản phẩm '{product.Name}' (Size: {variant.Size}, Màu: {variant.Color}, Giá bán: {sellingPrice:N0}₫) vào kho!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Inventory/ExportStock - Form xuất kho
        [HttpGet("ExportStock")]
        public async Task<IActionResult> ExportStock(int? productId)
        {
            ViewBag.Products = await _context.Products
                .Where(p => p.ProductVariants.Any(pv => pv.Quantity > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Nếu có productId, tự động chọn sản phẩm đó
            if (productId.HasValue)
            {
                ViewBag.SelectedProductId = productId.Value;
                var product = await _context.Products.FindAsync(productId.Value);
                ViewBag.SelectedProduct = product;
            }

            return View();
        }

        // POST: /Admin/Inventory/ExportStock - Xử lý xuất kho
        [HttpPost("ExportStock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportStock(int productId, int variantId, int quantity, string? reason)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0!";
                return RedirectToAction(nameof(ExportStock));
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(ExportStock));
            }

            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null || variant.ProductId != productId)
            {
                TempData["Error"] = "Không tìm thấy biến thể hợp lệ!";
                return RedirectToAction(nameof(ExportStock));
            }

            if (variant.Quantity < quantity)
            {
                TempData["Error"] = $"Không đủ hàng trong kho cho biến thể này! Hiện có: {variant.Quantity}";
                return RedirectToAction(nameof(ExportStock));
            }

            // Tạo giao dịch xuất kho
            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                ProductVariantId = variantId,
                Type = "Xuất",
                Quantity = quantity,
                Reason = reason ?? $"Xuất kho biến thể Size {variant.Size} - Màu {variant.Color}",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.InventoryTransactions.Add(transaction);

            // Cập nhật số lượng tồn kho
            variant.Quantity -= quantity;
            product.Quantity -= quantity;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xuất {quantity} sản phẩm '{product.Name}' (Size: {variant.Size}, Màu: {variant.Color}) khỏi kho!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Inventory/StockReport - Báo cáo tồn kho
        [HttpGet("StockReport")]
        public async Task<IActionResult> StockReport(string? search, string? category, string? sortBy)
        {
            var query = _context.Products.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
                ViewBag.Search = search;
            }

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
                ViewBag.Category = category;
            }

            // Sắp xếp
            query = sortBy switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "stock_asc" => query.OrderBy(p => p.ProductVariants.Sum(pv => pv.Quantity)),
                "stock_desc" => query.OrderByDescending(p => p.ProductVariants.Sum(pv => pv.Quantity)),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Name)
            };
            ViewBag.SortBy = sortBy;

            var products = await query.Include(p => p.ProductVariants).ToListAsync();

            // Thống kê
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalStock = products.Sum(p => p.Quantity);
            ViewBag.TotalValue = products.Sum(p => p.Quantity * p.Price);
            ViewBag.OutOfStock = products.Count(p => p.Quantity == 0);
            ViewBag.LowStock = products.Count(p => p.Quantity > 0 && p.Quantity <= 10);

            // Load danh mục
            ViewBag.Categories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => c.Name)
                .ToListAsync();

            return View(products);
        }

        // GET: /Admin/Inventory/ProductHistory/{id} - Lịch sử nhập/xuất của sản phẩm
        [HttpGet("ProductHistory/{id}")]
        public async Task<IActionResult> ProductHistory(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(StockReport));
            }

            var transactions = await _context.InventoryTransactions
                .Include(it => it.Creator)
                .Include(it => it.Order)
                .Where(it => it.ProductId == id)
                .OrderByDescending(it => it.CreatedAt)
                .ToListAsync();

            ViewBag.Product = product;
            ViewBag.TotalImport = transactions.Where(t => t.Type == "Nhập").Sum(t => t.Quantity);
            ViewBag.TotalExport = transactions.Where(t => t.Type == "Xuất").Sum(t => t.Quantity);

            return View(transactions);
        }

        // GET: /Admin/Inventory/EditProduct/{id} - Chỉnh sửa số lượng và giá
        [HttpGet("EditProduct/{id}")]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(LowStockAlert));
            }

            // Đồng bộ nếu chưa có biến thể
            if (product.ProductVariants == null || product.ProductVariants.Count == 0)
            {
                await ClothingShop.Common.ProductVariantHelper.SyncVariantsAsync(_context, product);
                product = await _context.Products
                    .Include(p => p.ProductVariants)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }

            return View(product);
        }

        // POST: /Admin/Inventory/UpdateProduct - Cập nhật số lượng và giá
        [HttpPost("UpdateProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(int id, Dictionary<int, int> variantQuantities, decimal? cost, decimal price)
        {
            var userIdString = HttpContext.Session.GetString(Constants.SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(LowStockAlert));
            }

            if (price <= 0)
            {
                TempData["Error"] = "Giá bán phải lớn hơn 0!";
                return RedirectToAction(nameof(EditProduct), new { id });
            }

            // Cập nhật các biến thể và ghi nhận giao dịch kho
            int totalQuantity = 0;
            if (variantQuantities != null)
            {
                foreach (var kv in variantQuantities)
                {
                    var variant = await _context.ProductVariants.FindAsync(kv.Key);
                    if (variant != null && variant.ProductId == id)
                    {
                        var oldQty = variant.Quantity;
                        var newQty = kv.Value;
                        if (newQty < 0) newQty = 0;

                        if (oldQty != newQty)
                        {
                            variant.Quantity = newQty;

                            var transaction = new InventoryTransaction
                            {
                                ProductId = id,
                                ProductVariantId = variant.Id,
                                Type = newQty > oldQty ? "Nhập" : "Xuất",
                                Quantity = Math.Abs(newQty - oldQty),
                                Cost = cost,
                                Reason = $"Chỉnh sửa biến thể (Size: {variant.Size}, Màu: {variant.Color}): {oldQty} → {newQty}",
                                CreatedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.InventoryTransactions.Add(transaction);
                        }

                        totalQuantity += newQty;
                    }
                }
            }
            else
            {
                totalQuantity = product.Quantity;
            }

            // Cập nhật giá bán & giá nhập của sản phẩm tổng thể
            product.Quantity = totalQuantity;
            product.Price = price;
            if (cost.HasValue)
            {
                product.Cost = cost.Value;
            }

            // Đồng thời cập nhật giá bán của các biến thể để đồng bộ với giá mới
            if (product.ProductVariants != null)
            {
                foreach (var variant in product.ProductVariants)
                {
                    variant.Price = price;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật tồn kho & giá cho sản phẩm '{product.Name}' thành công!";
            return RedirectToAction(nameof(LowStockAlert));
        }

        // GET: /Admin/Inventory/LowStockAlert - Danh sách tồn kho
        [HttpGet("LowStockAlert")]
        public async Task<IActionResult> LowStockAlert(string? search, string? sortBy)
        {
            var query = _context.Products.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Category.Contains(search));
                ViewBag.Search = search;
            }

            // Sắp xếp
            query = sortBy switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "stock_asc" => query.OrderBy(p => p.ProductVariants.Sum(pv => pv.Quantity)),
                "stock_desc" => query.OrderByDescending(p => p.ProductVariants.Sum(pv => pv.Quantity)),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.ProductVariants.Sum(pv => pv.Quantity)) // Mặc định: tồn kho thấp trước
            };
            ViewBag.SortBy = sortBy;

            var products = await query.Include(p => p.ProductVariants).ToListAsync();

            // Thống kê
            ViewBag.TotalProducts = products.Count;
            ViewBag.OutOfStock = products.Count(p => p.Quantity == 0);
            ViewBag.LowStock = products.Count(p => p.Quantity > 0 && p.Quantity <= 10);
            ViewBag.InStock = products.Count(p => p.Quantity > 10);

            return View(products);
        }
    }
}
