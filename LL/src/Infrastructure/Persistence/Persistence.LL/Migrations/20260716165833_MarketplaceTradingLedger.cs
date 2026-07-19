using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceTradingLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SellerName",
                table: "MarketPlaceListings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "MarketPlaceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    TotalPrice = table.Column<long>(type: "bigint", nullable: false),
                    SellerFee = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceOrders_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_SellerId",
                table: "MarketPlaceListings",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_UnitPrice_CreatedAt",
                table: "MarketPlaceListings",
                columns: new[] { "UnitPrice", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_BuyerId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "BuyerId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_ItemBaseId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "ItemBaseId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_SellerId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "SellerId", "PurchasedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketPlaceOrders");

            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceListings_SellerId",
                table: "MarketPlaceListings");

            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceListings_UnitPrice_CreatedAt",
                table: "MarketPlaceListings");

            migrationBuilder.AlterColumn<string>(
                name: "SellerName",
                table: "MarketPlaceListings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
