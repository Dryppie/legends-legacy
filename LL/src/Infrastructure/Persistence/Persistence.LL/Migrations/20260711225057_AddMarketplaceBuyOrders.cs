using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceBuyOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketPlaceBuyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceBuyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceBuyOrders_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_BuyerId",
                table: "MarketPlaceBuyOrders",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_ItemBaseId_UnitPrice_CreatedAt",
                table: "MarketPlaceBuyOrders",
                columns: new[] { "ItemBaseId", "UnitPrice", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketPlaceBuyOrders");
        }
    }
}
