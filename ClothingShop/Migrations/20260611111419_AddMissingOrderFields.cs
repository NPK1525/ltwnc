using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Address')
                BEGIN
                    ALTER TABLE [Orders] ADD [Address] nvarchar(500) NULL;
                END
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Note')
                BEGIN
                    ALTER TABLE [Orders] ADD [Note] nvarchar(1000) NULL;
                END
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PhoneNumber')
                BEGIN
                    ALTER TABLE [Orders] ADD [PhoneNumber] nvarchar(20) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Address')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [Address];
                END
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'Note')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [Note];
                END
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Orders]') AND name = 'PhoneNumber')
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [PhoneNumber];
                END
            ");
        }
    }
}
