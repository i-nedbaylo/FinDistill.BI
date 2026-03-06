using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinDistill.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dwh");

            migrationBuilder.EnsureSchema(
                name: "lake");

            migrationBuilder.CreateTable(
                name: "DimAssets",
                schema: "dwh",
                columns: table => new
                {
                    AssetKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimAssets", x => x.AssetKey);
                });

            migrationBuilder.CreateTable(
                name: "DimDates",
                schema: "dwh",
                columns: table => new
                {
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    FullDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Quarter = table.Column<byte>(type: "tinyint", nullable: false),
                    Month = table.Column<byte>(type: "tinyint", nullable: false),
                    Day = table.Column<byte>(type: "tinyint", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    WeekOfYear = table.Column<byte>(type: "tinyint", nullable: false),
                    IsWeekend = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimDates", x => x.DateKey);
                });

            migrationBuilder.CreateTable(
                name: "DimSources",
                schema: "dwh",
                columns: table => new
                {
                    SourceKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimSources", x => x.SourceKey);
                });

            migrationBuilder.CreateTable(
                name: "RawIngestData",
                schema: "lake",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RawContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawIngestData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactQuotes",
                schema: "dwh",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetKey = table.Column<int>(type: "int", nullable: false),
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    SourceKey = table.Column<int>(type: "int", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HighPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LowPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactQuotes_DimAssets_AssetKey",
                        column: x => x.AssetKey,
                        principalSchema: "dwh",
                        principalTable: "DimAssets",
                        principalColumn: "AssetKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactQuotes_DimDates_DateKey",
                        column: x => x.DateKey,
                        principalSchema: "dwh",
                        principalTable: "DimDates",
                        principalColumn: "DateKey",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactQuotes_DimSources_SourceKey",
                        column: x => x.SourceKey,
                        principalSchema: "dwh",
                        principalTable: "DimSources",
                        principalColumn: "SourceKey",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DimAssets_Ticker",
                schema: "dwh",
                table: "DimAssets",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimDates_FullDate",
                schema: "dwh",
                table: "DimDates",
                column: "FullDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimSources_SourceName",
                schema: "dwh",
                table: "DimSources",
                column: "SourceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactQuotes_Asset_Date_Source",
                schema: "dwh",
                table: "FactQuotes",
                columns: new[] { "AssetKey", "DateKey", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactQuotes_DateKey",
                schema: "dwh",
                table: "FactQuotes",
                column: "DateKey");

            migrationBuilder.CreateIndex(
                name: "IX_FactQuotes_SourceKey",
                schema: "dwh",
                table: "FactQuotes",
                column: "SourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_RawIngestData_Unprocessed",
                schema: "lake",
                table: "RawIngestData",
                column: "IsProcessed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactQuotes",
                schema: "dwh");

            migrationBuilder.DropTable(
                name: "RawIngestData",
                schema: "lake");

            migrationBuilder.DropTable(
                name: "DimAssets",
                schema: "dwh");

            migrationBuilder.DropTable(
                name: "DimDates",
                schema: "dwh");

            migrationBuilder.DropTable(
                name: "DimSources",
                schema: "dwh");
        }
    }
}
