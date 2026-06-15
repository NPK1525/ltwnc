using ClothingShop.Models;
using ClothingShop.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.Services
{
    public class CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context) : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ApplicationDbContext _context = context;

        private int? GetUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return null;
            }
            return userId;
        }

        // 1. BẮT BUỘC PHẢI CÓ
        public List<CartItem> GetCartItems()
        {
            var userId = GetUserId();
            if (userId == null) return [];

            var carts = _context.Carts
                .Where(c => c.UserId == userId.Value)
                .Include(c => c.Product)
                .ToList();

            var cartItems = new List<CartItem>();
            bool hasChanges = false;

            foreach (var c in carts)
            {
                var variant = _context.ProductVariants
                    .FirstOrDefault(pv => pv.ProductId == c.ProductId && pv.Size == c.Size && pv.Color == c.Color);

                if (variant == null)
                {
                    _context.Carts.Remove(c);
                    hasChanges = true;
                    continue;
                }

                // Nếu số lượng trong giỏ lớn hơn tồn kho hiện tại, tự động cập nhật giảm về bằng số lượng tồn kho
                if (c.Quantity > variant.Quantity)
                {
                    c.Quantity = variant.Quantity;
                    hasChanges = true;
                }

                if (c.Quantity <= 0)
                {
                    _context.Carts.Remove(c);
                    hasChanges = true;
                    continue;
                }

                cartItems.Add(new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Product!.Name,
                    Price = variant.Price > 0 ? variant.Price : c.Product.Price,
                    ImageUrl = c.Product.ImageUrl,
                    Quantity = c.Quantity,
                    Size = c.Size,
                    Color = c.Color
                });
            }

            if (hasChanges)
            {
                _context.SaveChanges();
            }

            return cartItems;
        }

        public void AddToCart(int productId, int quantity = 1, string? selectedSize = null, string? selectedColor = null)
        {
            var userId = GetUserId();
            if (userId == null) return;

            var product = _context.Products.Find(productId);
            if (product == null) return;

            // Kiểm tra số lượng tồn kho theo biến thể
            var variant = _context.ProductVariants
                .FirstOrDefault(pv => pv.ProductId == productId && pv.Size == selectedSize && pv.Color == selectedColor);
            if (variant == null) return;

            // Tìm cart item hiện có
            var existingCart = _context.Carts
                .FirstOrDefault(c => c.UserId == userId.Value
                    && c.ProductId == productId
                    && c.Size == selectedSize
                    && c.Color == selectedColor);

            if (existingCart != null)
            {
                var newQty = existingCart.Quantity + quantity;
                if (newQty > variant.Quantity)
                {
                    existingCart.Quantity = variant.Quantity;
                }
                else
                {
                    existingCart.Quantity = newQty;
                }
            }
            else
            {
                if (quantity > variant.Quantity)
                {
                    quantity = variant.Quantity;
                }
                if (quantity > 0)
                {
                    var newCart = new Cart
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        Quantity = quantity,
                        Size = selectedSize,
                        Color = selectedColor,
                        AddedDate = DateTime.Now
                    };
                    _context.Carts.Add(newCart);
                }
            }
            _context.SaveChanges();
        }

        public void UpdateQuantity(int productId, int quantity, string? size = null, string? color = null)
        {
            var userId = GetUserId();
            if (userId == null) return;

            var normalizedSize = size == "N/A" ? null : size;
            var normalizedColor = color == "N/A" ? null : color;

            // Nếu quantity <= 0, xóa sản phẩm khỏi giỏ hàng
            if (quantity <= 0)
            {
                RemoveFromCart(productId, size, color);
                return;
            }

            // Kiểm tra tồn kho theo biến thể
            var variant = _context.ProductVariants
                .FirstOrDefault(pv => pv.ProductId == productId && pv.Size == normalizedSize && pv.Color == normalizedColor);
            if (variant == null) return;

            // Không cho phép cập nhật số lượng vượt quá tồn kho của biến thể
            if (quantity > variant.Quantity)
            {
                quantity = variant.Quantity;
            }

            var cartItem = _context.Carts
                .FirstOrDefault(c => c.UserId == userId.Value
                    && c.ProductId == productId
                    && c.Size == normalizedSize
                    && c.Color == normalizedColor);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                _context.SaveChanges();
            }
        }

        public void RemoveFromCart(int productId, string? size = null, string? color = null)
        {
            var userId = GetUserId();
            if (userId == null) return;

            var normalizedSize = size == "N/A" ? null : size;
            var normalizedColor = color == "N/A" ? null : color;

            var cartItems = _context.Carts
                .Where(c => c.UserId == userId.Value
                    && c.ProductId == productId
                    && c.Size == normalizedSize
                    && c.Color == normalizedColor)
                .ToList();

            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();
        }

        public void ClearCart()
        {
            var userId = GetUserId();
            if (userId == null) return;

            var cartItems = _context.Carts.Where(c => c.UserId == userId.Value).ToList();
            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();
        }

        public int GetTotalItems()
        {
            return GetCartItems().Sum(x => x.Quantity);
        }

        public decimal GetTotalPrice()
        {
            return GetCartItems().Sum(x => x.Price * x.Quantity);
        }

        public async Task<List<CartItem>> GetBuyNowCartItemAsync(int productId, int quantity = 1, string? size = null, string? color = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return [];

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.Size == size && pv.Color == color);
            decimal price = variant != null && variant.Price > 0 ? variant.Price : product.Price;

            return [
                new()
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    Size = size,
                    Color = color
                }
            ];
        }
    }
}