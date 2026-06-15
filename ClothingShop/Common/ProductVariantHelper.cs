using ClothingShop.Data;
using ClothingShop.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClothingShop.Common
{
    public static class ProductVariantHelper
    {
        public static List<string> ParseStringList(string? jsonOrStr)
        {
            var list = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(jsonOrStr))
                {
                    if (jsonOrStr.TrimStart().StartsWith('['))
                    {
                        list = JsonSerializer.Deserialize<List<string>>(jsonOrStr) ?? [];
                    }
                    else
                    {
                        list = [jsonOrStr];
                    }
                }
            }
            catch
            {
                list = [];
            }
            return list;
        }

        private static readonly List<string> SizeOrder = ["S", "M", "L", "XL", "XXL", "XXXL", "Freesize", "Không có size"];

        public static List<string> SortSizes(IEnumerable<string>? sizes)
        {
            if (sizes == null) return [];
            return sizes
                .OrderBy(s =>
                {
                    var index = SizeOrder.IndexOf(s);
                    return index >= 0 ? index : 999;
                })
                .ThenBy(s => s)
                .ToList();
        }

        public static List<string> GetActiveSizes(Product product)
        {
            var sizes = ParseStringList(product.Size);
            if (sizes.Count == 0) sizes.Add("M");
            return SortSizes(sizes);
        }

        public static List<string> GetActiveColors(Product product)
        {
            var colors = ParseStringList(product.Color);
            if (colors.Count == 0) colors.Add("Đen");
            return colors;
        }

        public static IEnumerable<ProductVariant> FilterActiveVariants(Product product)
        {
            if (product.ProductVariants == null) return [];
            var sizes = GetActiveSizes(product);
            var colors = GetActiveColors(product);
            var validVariantKeys = new HashSet<(string size, string color)>(
                sizes.SelectMany(s => colors.Select(c => (s, c)))
            );
            return product.ProductVariants.Where(pv => validVariantKeys.Contains((pv.Size, pv.Color)));
        }

        public static async Task SyncVariantsAsync(ApplicationDbContext context, Product product)
        {
            // Parse Size và Color
            var sizes = GetActiveSizes(product);
            var colors = GetActiveColors(product);

            // Lấy tất cả các biến thể hiện có trong DB
            var existingVariants = await context.ProductVariants
                .Where(pv => pv.ProductId == product.Id)
                .ToListAsync();

            // Tập hợp các cấu hình hợp lệ mới
            var validVariantKeys = new HashSet<(string size, string color)>();
            foreach (var size in sizes)
            {
                foreach (var color in colors)
                {
                    validVariantKeys.Add((size, color));
                }
            }

            // 1. Xóa hoặc vô hiệu hóa các biến thể không còn hợp lệ
            var candidatesToRemove = existingVariants
                .Where(ev => !validVariantKeys.Contains((ev.Size, ev.Color)))
                .ToList();

            var variantsToRemove = new List<ProductVariant>();

            foreach (var ev in candidatesToRemove)
            {
                // Kiểm tra xem biến thể có tham chiếu từ InventoryTransactions hoặc ProductVariantBatches không
                var hasTransactions = await context.InventoryTransactions.AnyAsync(it => it.ProductVariantId == ev.Id);
                var hasBatches = await context.ProductVariantBatches.AnyAsync(pb => pb.ProductVariantId == ev.Id);

                if (!hasTransactions && !hasBatches)
                {
                    variantsToRemove.Add(ev);
                }
                else
                {
                    // Nếu có tham chiếu, chúng ta không thể xóa do ràng buộc FK.
                    // Đặt số lượng về 0 và lưu lại để tránh việc tiếp tục bán hoặc hiển thị sai.
                    ev.Quantity = 0;
                }
            }

            if (variantsToRemove.Count > 0)
            {
                context.ProductVariants.RemoveRange(variantsToRemove);
            }

            // 2. Thêm các biến thể mới chưa tồn tại
            var existingKeys = existingVariants
                .Where(ev => validVariantKeys.Contains((ev.Size, ev.Color)))
                .Select(ev => (ev.Size, ev.Color))
                .ToHashSet();

            foreach (var key in validVariantKeys)
            {
                if (!existingKeys.Contains(key))
                {
                    var newVariant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Size = key.size,
                        Color = key.color,
                        Quantity = 0 // Khởi tạo với số lượng bằng 0 (Admin sẽ nhập kho sau)
                    };
                    context.ProductVariants.Add(newVariant);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
