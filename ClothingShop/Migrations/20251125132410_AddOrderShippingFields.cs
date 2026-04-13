using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ thêm FullName vì các cột khác đã tồn tại
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'FullName')
                BEGIN
                    ALTER TABLE [Orders] ADD [FullName] nvarchar(100) NULL;
                END
            ");
            
            // Cập nhật độ dài cho các cột đã tồn tại
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PhoneNumber')
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [PhoneNumber] nvarchar(20) NULL;
                END
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Address')
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [Address] nvarchar(500) NULL;
                END
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Note')
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [Note] nvarchar(1000) NULL;
                END
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'CancelReason')
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [CancelReason] nvarchar(500) NULL;
                END
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'CancelledBy')
                BEGIN
                    ALTER TABLE [Orders] ALTER COLUMN [CancelledBy] nvarchar(20) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ xóa FullName vì đó là cột mới thêm
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'FullName')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [FullName];
                END
            ");
        }
    }
}
