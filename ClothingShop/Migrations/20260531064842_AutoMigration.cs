using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Migrations
{
    /// <inheritdoc />
    public partial class AutoMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaidAt')
                BEGIN
                    ALTER TABLE [Orders] ADD [PaidAt] datetime2 NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentMethod')
                BEGIN
                    ALTER TABLE [Orders] ADD [PaymentMethod] nvarchar(max) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentStatus')
                BEGIN
                    ALTER TABLE [Orders] ADD [PaymentStatus] nvarchar(max) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentTransactionId')
                BEGIN
                    ALTER TABLE [Orders] ADD [PaymentTransactionId] nvarchar(max) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaidAt')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [PaidAt];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentMethod')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [PaymentMethod];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentStatus')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [PaymentStatus];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PaymentTransactionId')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [PaymentTransactionId];
                END
            ");
        }
    }
}
