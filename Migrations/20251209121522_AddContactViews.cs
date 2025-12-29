using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandPortal.Api.Migrations
{
    public partial class AddContactViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IsCover: add only if missing
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','IsCover') IS NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia
    ADD IsCover bit NOT NULL CONSTRAINT DF_PropertyMedia_IsCover DEFAULT (0);
END
");

            // keep your existing alterations to Width/Height
            migrationBuilder.AlterColumn<int>(
                name: "Width",
                table: "PropertyMedia",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Height",
                table: "PropertyMedia",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Path (nvarchar(max)) — add if missing
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','Path') IS NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia
    ADD Path nvarchar(max) NULL;
END
");

            // PublicUrl (nvarchar(max)) — add if missing
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','PublicUrl') IS NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia
    ADD PublicUrl nvarchar(max) NULL;
END
");

            // Properties table: optional extra fields — add only if missing
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','Brokerage') IS NULL
BEGIN
    ALTER TABLE dbo.Properties
    ADD Brokerage nvarchar(max) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','Facing') IS NULL
BEGIN
    ALTER TABLE dbo.Properties
    ADD Facing nvarchar(max) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','PlotType') IS NULL
BEGIN
    ALTER TABLE dbo.Properties
    ADD PlotType nvarchar(max) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','RoadAccess') IS NULL
BEGIN
    ALTER TABLE dbo.Properties
    ADD RoadAccess nvarchar(max) NULL;
END
");

            // Create ContactViews table (EF CreateTable is safe; if you re-ran migrations previously
            // this will only run once because EF tracks applied migrations)
            migrationBuilder.CreateTable(
                name: "ContactViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPremiumAccess = table.Column<bool>(type: "bit", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactViews", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop ContactViews table if it exists (EF will drop it if present)
            migrationBuilder.DropTable(
                name: "ContactViews");

            // Drop IsCover if it exists
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','IsCover') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia DROP CONSTRAINT IF EXISTS DF_PropertyMedia_IsCover;
    ALTER TABLE dbo.PropertyMedia DROP COLUMN IsCover;
END
");

            // Drop Path if exists
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','Path') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia DROP COLUMN Path;
END
");

            // Drop PublicUrl if exists
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.PropertyMedia','PublicUrl') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PropertyMedia DROP COLUMN PublicUrl;
END
");

            // Drop extras on Properties table if they exist
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','Brokerage') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Properties DROP COLUMN Brokerage;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','Facing') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Properties DROP COLUMN Facing;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','PlotType') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Properties DROP COLUMN PlotType;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Properties','RoadAccess') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Properties DROP COLUMN RoadAccess;
END
");

            // revert Width/Height to previous state (as your original Down() did)
            migrationBuilder.AlterColumn<int>(
                name: "Width",
                table: "PropertyMedia",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Height",
                table: "PropertyMedia",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
