using ClothingShop.Data;
using ClothingShop.Services;
using ClothingShop.Models;
using ClothingShop.Models.Enums;
using ClothingShop.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// SQL SERVER
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session + HttpContextAccessor
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(Constants.Auth.SessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// QUAN TRỌNG: ĐĂNG KÝ CART SERVICE
builder.Services.AddScoped<ICartService, CartService>();

// ĐĂNG KÝ EMAIL SERVICE
builder.Services.AddScoped<IEmailService, EmailService>();

// ĐĂNG KÝ VNPAY SERVICE
builder.Services.AddScoped<IVNPayService, VNPayService>();

// ĐĂNG KÝ ORDER SERVICE
builder.Services.AddScoped<IOrderService, OrderService>();

// AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(Constants.Auth.SessionTimeoutMinutes);
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

// MVC
builder.Services.AddControllersWithViews();

// CONFIG RATE LIMITING
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// TỰ ĐỘNG TẠO DB + ADMIN
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var adminEmail = "gamer957ola@gmail.com";
    if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var admin = new User
        {
            FullName = "Admin Khang",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Khang@123", workFactor: 12),
            PhoneNumber = "0901234567",
            Gender = "Nam",
            IsAdmin = true,
            CreatedAt = DateTime.Now
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    // TỰ ĐỘNG KHỞI TẠO BIẾN THỂ CHO SẢN PHẨM CŨ (ĐẢM BẢO KHÔNG MẤT DỮ LIỆU)
    var products = await db.Products.Include(p => p.ProductVariants).ToListAsync();
    foreach (var product in products)
    {
        if (product.ProductVariants.Count == 0)
        {
            await ProductVariantHelper.SyncVariantsAsync(db, product);
        }
    }

    // TỰ ĐỘNG KHỞI TẠO VOUCHER MẪU ĐỂ TEST
    if (!await db.Vouchers.AnyAsync())
    {
        db.Vouchers.AddRange(
            new Voucher
            {
                Code = "SUMMER20",
                DiscountType = VoucherDiscountType.Percentage,
                DiscountValue = 20,
                MaxDiscountAmount = 50000,
                MinOrderAmount = 200000,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(30),
                UsageLimit = 100,
                LimitPerUser = 1,
                Description = "Giảm 20% cho đơn hàng từ 200k (tối đa 50k)",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new Voucher
            {
                Code = "FREESHIP",
                DiscountType = VoucherDiscountType.FixedAmount,
                DiscountValue = 30000,
                MinOrderAmount = 150000,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(30),
                UsageLimit = 200,
                LimitPerUser = 2,
                Description = "Giảm 30k phí ship cho đơn hàng từ 150k",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        );
        await db.SaveChangesAsync();
    }
}

// Middleware
app.UseHttpsRedirection();
app.UseIpRateLimiting();

// Cấu hình MIME type cho AVIF
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".avif"] = "image/avif";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Middleware để ngăn cache cho các trang authenticated
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true ||
        context.Session.GetString(Constants.SessionKeys.UserId) != null)
    {
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();