using Microsoft.EntityFrameworkCore;
using ClothingShop.Models;

namespace ClothingShop.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {

        // DB SETS
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<ProductReview> ProductReviews { get; set; } = null!;
        // ❌ REMOVED: public DbSet<PaymentInfo> PaymentInfos { get; set; } = null!;
        // ❌ REMOVED: public DbSet<FashionCategory> FashionCategories { get; set; } = null!;
        public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<SupportTicket> SupportTickets { get; set; } = null!;
        public DbSet<SupportMessage> SupportMessages { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
        public DbSet<ProductView> ProductViews { get; set; } = null!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
        public DbSet<ProductVariantBatch> ProductVariantBatches { get; set; } = null!;
        public DbSet<Voucher> Vouchers { get; set; } = null!;
        public DbSet<VoucherUsage> VoucherUsages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(p => p.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                entity.Property(p => p.ImageUrl)
                      .HasMaxLength(500);
                entity.Property(p => p.Description)
                      .HasMaxLength(1000);
                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.FullName)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.HasIndex(u => u.Email)
                      .IsUnique();
                entity.Property(u => u.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(256);
                entity.Property(u => u.PhoneNumber)
                      .IsRequired()
                      .HasMaxLength(20);
                entity.Property(u => u.Gender)
                      .HasMaxLength(10);
                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(u => u.IsAdmin)
                      .HasDefaultValue(false);
            });

            // CẤU HÌNH ORDER
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(o => o.Status).HasMaxLength(20);
                entity.Property(o => o.OrderDate).HasDefaultValueSql("GETDATE()");
                entity.Property(o => o.FullName).HasMaxLength(100);
                entity.Property(o => o.PhoneNumber).HasMaxLength(20);
                entity.Property(o => o.Address).HasMaxLength(500);
                entity.Property(o => o.Note).HasMaxLength(1000);
                entity.Property(o => o.CancelReason).HasMaxLength(500);
                entity.Property(o => o.CancelledBy).HasMaxLength(20);
                entity.Property(o => o.VoucherCode).HasMaxLength(50);
                entity.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            });

            // CẤU HÌNH ORDERITEM – SỬA TÊN BIẾN 'oi'
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.Price).HasColumnType("decimal(18,2)");
                entity.Property(oi => oi.ProductName).HasMaxLength(200);
            });

            // CẤU HÌNH PRODUCTREVIEW
            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Rating).IsRequired();
                entity.Property(r => r.Comment).HasMaxLength(1000);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETDATE()");

                // Relationships
                entity.HasOne(r => r.Product)
                      .WithMany()
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ❌ REMOVED: CẤU HÌNH PAYMENTINFO (đã chuyển sang appsettings.json)

            // ❌ REMOVED: CẤU HÌNH FASHIONCATEGORY (đã merge vào ProductCategory)

            // CẤU HÌNH PRODUCTCATEGORY
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(pc => pc.Id);
                entity.Property(pc => pc.Name).IsRequired().HasMaxLength(100);
                entity.Property(pc => pc.Description).HasMaxLength(500);
                entity.Property(pc => pc.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // CẤU HÌNH NOTIFICATION
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
                entity.Property(n => n.Type).HasMaxLength(20);
                entity.Property(n => n.IsRead).HasDefaultValue(false);
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // CẤU HÌNH WISHLISTITEM
            modelBuilder.Entity<WishlistItem>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.AddedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(w => w.User)
                      .WithMany()
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Product)
                      .WithMany()
                      .HasForeignKey(w => w.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Đảm bảo mỗi user chỉ có 1 wishlist item cho mỗi product
                entity.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            });

            // CẤU HÌNH CART
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Quantity).IsRequired();
                entity.Property(c => c.Size).HasMaxLength(20);
                entity.Property(c => c.Color).HasMaxLength(50);
                entity.Property(c => c.AddedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Product)
                      .WithMany()
                      .HasForeignKey(c => c.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Đảm bảo mỗi user chỉ có 1 cart item cho mỗi product+size+color
                entity.HasIndex(c => new { c.UserId, c.ProductId, c.Size, c.Color }).IsUnique();
            });

            // CẤU HÌNH SUPPORT TICKET
            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Subject).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Status).IsRequired().HasMaxLength(20);
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // CẤU HÌNH SUPPORT MESSAGE
            modelBuilder.Entity<SupportMessage>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Message).IsRequired().HasMaxLength(2000);
                entity.Property(m => m.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(m => m.Ticket)
                      .WithMany(t => t.Messages)
                      .HasForeignKey(m => m.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CẤU HÌNH INVENTORY TRANSACTION
            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.HasKey(it => it.Id);
                entity.Property(it => it.Type).IsRequired().HasMaxLength(20);
                entity.Property(it => it.Quantity).IsRequired();
                entity.Property(it => it.Reason).HasMaxLength(500);
                entity.Property(it => it.Supplier).HasMaxLength(100);
                entity.Property(it => it.Cost).HasColumnType("decimal(18,2)");
                entity.Property(it => it.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(it => it.Product)
                      .WithMany()
                      .HasForeignKey(it => it.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(it => it.Creator)
                      .WithMany()
                      .HasForeignKey(it => it.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(it => it.Order)
                      .WithMany()
                      .HasForeignKey(it => it.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(it => it.ProductVariant)
                      .WithMany()
                      .HasForeignKey(it => it.ProductVariantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CẤU HÌNH PRODUCT VARIANT
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.HasKey(pv => pv.Id);
                entity.Property(pv => pv.Size).IsRequired().HasMaxLength(50);
                entity.Property(pv => pv.Color).IsRequired().HasMaxLength(50);
                entity.Property(pv => pv.Quantity).IsRequired().HasDefaultValue(0);
                entity.Property(pv => pv.Price).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0);

                entity.HasOne(pv => pv.Product)
                      .WithMany(p => p.ProductVariants)
                      .HasForeignKey(pv => pv.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Đảm bảo cặp Size + Color là duy nhất cho mỗi sản phẩm
                entity.HasIndex(pv => new { pv.ProductId, pv.Size, pv.Color }).IsUnique();
            });

            // CẤU HÌNH PRODUCT VARIANT BATCH (LÔ HÀNG)
            modelBuilder.Entity<ProductVariantBatch>(entity =>
            {
                entity.HasKey(pb => pb.Id);
                entity.Property(pb => pb.BatchNumber).IsRequired().HasMaxLength(50);
                entity.Property(pb => pb.Cost).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(pb => pb.ImportQuantity).IsRequired();
                entity.Property(pb => pb.RemainingQuantity).IsRequired();
                entity.Property(pb => pb.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(pb => pb.ProductVariant)
                      .WithMany()
                      .HasForeignKey(pb => pb.ProductVariantId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index để tìm kiếm theo FIFO nhanh hơn
                entity.HasIndex(pb => new { pb.ProductVariantId, pb.RemainingQuantity, pb.CreatedAt });
            });

            // CẤU HÌNH PRODUCT VIEW
            modelBuilder.Entity<ProductView>(entity =>
            {
                entity.HasKey(pv => pv.Id);
                entity.Property(pv => pv.SessionId).HasMaxLength(50);
                entity.Property(pv => pv.ViewedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(pv => pv.User)
                      .WithMany()
                      .HasForeignKey(pv => pv.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pv => pv.Product)
                      .WithMany()
                      .HasForeignKey(pv => pv.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index để tìm kiếm nhanh
                entity.HasIndex(pv => new { pv.UserId, pv.ViewedAt });
                entity.HasIndex(pv => new { pv.SessionId, pv.ViewedAt });
            });

            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Code).IsRequired().HasMaxLength(50);
                entity.HasIndex(v => v.Code).IsUnique();
                entity.Property(v => v.DiscountValue).HasColumnType("decimal(18,2)");
                entity.Property(v => v.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(v => v.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(v => v.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<VoucherUsage>(entity =>
            {
                entity.HasKey(vu => vu.Id);
                entity.Property(vu => vu.UsedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(vu => vu.Voucher)
                      .WithMany()
                      .HasForeignKey(vu => vu.VoucherId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vu => vu.User)
                      .WithMany()
                      .HasForeignKey(vu => vu.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(vu => vu.Order)
                      .WithMany()
                      .HasForeignKey(vu => vu.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}