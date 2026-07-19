using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceOrderExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "MarketPlaceListings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "MarketPlaceBuyOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE "MarketPlaceListings"
                SET "ExpiresAt" = "CreatedAt" + INTERVAL '7 days';

                UPDATE "MarketPlaceBuyOrders"
                SET "ExpiresAt" = "CreatedAt" + INTERVAL '7 days';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_ExpiresAt",
                table: "MarketPlaceListings",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_ExpiresAt",
                table: "MarketPlaceBuyOrders",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceListings_ExpiresAt",
                table: "MarketPlaceListings");

            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceBuyOrders_ExpiresAt",
                table: "MarketPlaceBuyOrders");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "MarketPlaceListings");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "MarketPlaceBuyOrders");
        }
    }
}
