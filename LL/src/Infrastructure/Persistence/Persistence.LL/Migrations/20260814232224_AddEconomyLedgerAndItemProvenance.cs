using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyLedgerAndItemProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuyerAccountId",
                table: "MarketPlaceOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerAccountId",
                table: "MarketPlaceOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcquiredAtUtc",
                table: "ItemInstances",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionSource",
                table: "ItemInstances",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "legacy-backfill");

            migrationBuilder.CreateTable(
                name: "EconomyLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    AssetType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderAccountCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SenderCharacterLevel = table.Column<int>(type: "integer", nullable: true),
                    RecipientAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientAccountCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecipientCharacterLevel = table.Column<int>(type: "integer", nullable: true),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AssetName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SourceItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    UnitValue = table.Column<long>(type: "bigint", nullable: true),
                    TotalValue = table.Column<long>(type: "bigint", nullable: true),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: true),
                    RiskDecision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RuleHits = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomyLedger", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                UPDATE "MarketPlaceOrders" AS orders
                SET "SellerAccountId" = seller."UserId",
                    "BuyerAccountId" = buyer."UserId"
                FROM "Entities" AS seller, "Entities" AS buyer
                WHERE seller."Id" = orders."SellerId"
                  AND buyer."Id" = orders."BuyerId";
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "EconomyLedger" (
                    "Id", "EventType", "AssetType", "ReferenceId",
                    "SenderAccountId", "SenderCharacterId", "SenderAccountCreatedUtc", "SenderCharacterLevel",
                    "RecipientAccountId", "RecipientCharacterId", "RecipientAccountCreatedUtc", "RecipientCharacterLevel",
                    "AssetId", "AssetName", "SourceItemInstanceId", "DestinationItemInstanceId",
                    "Quantity", "UnitValue", "TotalValue", "Source", "OccurredAt")
                SELECT
                    transfer."Id",
                    CASE transfer."Kind"
                        WHEN 'InventoryItem' THEN 'DirectItemTransfer'
                        WHEN 'Cinders' THEN 'DirectCurrencyTransfer'
                    END,
                    CASE transfer."Kind"
                        WHEN 'InventoryItem' THEN 'Item'
                        WHEN 'Cinders' THEN 'Currency'
                    END,
                    transfer."Id",
                    transfer."SenderAccountId", transfer."SenderCharacterId", sender_user."CreatedUtc", sender."Level",
                    transfer."RecipientAccountId", transfer."RecipientCharacterId", recipient_user."CreatedUtc", recipient."Level",
                    transfer."AssetId", transfer."AssetName", transfer."SourceItemInstanceId", transfer."DestinationItemInstanceId",
                    transfer."Quantity",
                    CASE WHEN transfer."Kind" = 'Cinders' THEN 1 ELSE NULL END,
                    CASE WHEN transfer."Kind" = 'Cinders' THEN transfer."Quantity" ELSE NULL END,
                    CASE WHEN transfer."Kind" = 'Cinders' THEN 'player-wire:legacy-backfill' ELSE 'player-transfer:legacy-backfill' END,
                    transfer."OccurredAt"
                FROM "PlayerTransferHistory" AS transfer
                LEFT JOIN "Entities" AS sender ON sender."Id" = transfer."SenderCharacterId"
                LEFT JOIN "Entities" AS recipient ON recipient."Id" = transfer."RecipientCharacterId"
                LEFT JOIN "Users" AS sender_user ON sender_user."Id" = transfer."SenderAccountId"
                LEFT JOIN "Users" AS recipient_user ON recipient_user."Id" = transfer."RecipientAccountId"
                WHERE transfer."Kind" IN ('InventoryItem', 'Cinders');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "EconomyLedger" (
                    "Id", "EventType", "AssetType", "ReferenceId",
                    "SenderAccountId", "SenderCharacterId", "SenderAccountCreatedUtc", "SenderCharacterLevel",
                    "RecipientAccountId", "RecipientCharacterId", "RecipientAccountCreatedUtc", "RecipientCharacterLevel",
                    "AssetId", "AssetName", "SourceItemInstanceId", "DestinationItemInstanceId",
                    "Quantity", "UnitValue", "TotalValue", "Source", "OccurredAt")
                SELECT
                    md5(orders."Id"::text || ':item')::uuid,
                    'MarketplaceTrade', 'Item', orders."Id",
                    orders."SellerAccountId", orders."SellerId", seller_user."CreatedUtc", seller."Level",
                    orders."BuyerAccountId", orders."BuyerId", buyer_user."CreatedUtc", buyer."Level",
                    orders."ItemBaseId", item."Name", orders."ItemInstanceId", orders."ItemInstanceId",
                    orders."Quantity", orders."UnitPrice", orders."TotalPrice",
                    'marketplace:' || CASE orders."Source" WHEN 0 THEN 'SellListing' ELSE 'BuyOrder' END || ':legacy-backfill',
                    orders."PurchasedAt"
                FROM "MarketPlaceOrders" AS orders
                JOIN "ItemBases" AS item ON item."Id" = orders."ItemBaseId"
                LEFT JOIN "Entities" AS seller ON seller."Id" = orders."SellerId"
                LEFT JOIN "Entities" AS buyer ON buyer."Id" = orders."BuyerId"
                LEFT JOIN "Users" AS seller_user ON seller_user."Id" = orders."SellerAccountId"
                LEFT JOIN "Users" AS buyer_user ON buyer_user."Id" = orders."BuyerAccountId";
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "EconomyLedger" (
                    "Id", "EventType", "AssetType", "ReferenceId",
                    "SenderAccountId", "SenderCharacterId", "SenderAccountCreatedUtc", "SenderCharacterLevel",
                    "RecipientAccountId", "RecipientCharacterId", "RecipientAccountCreatedUtc", "RecipientCharacterLevel",
                    "AssetId", "AssetName", "Quantity", "UnitValue", "TotalValue", "Source", "OccurredAt")
                SELECT
                    md5(orders."Id"::text || ':payment')::uuid,
                    'MarketplaceTrade', 'Currency', orders."Id",
                    orders."BuyerAccountId", orders."BuyerId", buyer_user."CreatedUtc", buyer."Level",
                    orders."SellerAccountId", orders."SellerId", seller_user."CreatedUtc", seller."Level",
                    'currency:cinders', 'Cinders', orders."TotalPrice", 1, orders."TotalPrice",
                    'marketplace:' || CASE orders."Source" WHEN 0 THEN 'SellListing' ELSE 'BuyOrder' END || ':payment:legacy-backfill',
                    orders."PurchasedAt"
                FROM "MarketPlaceOrders" AS orders
                LEFT JOIN "Entities" AS seller ON seller."Id" = orders."SellerId"
                LEFT JOIN "Entities" AS buyer ON buyer."Id" = orders."BuyerId"
                LEFT JOIN "Users" AS seller_user ON seller_user."Id" = orders."SellerAccountId"
                LEFT JOIN "Users" AS buyer_user ON buyer_user."Id" = orders."BuyerAccountId";
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "EconomyLedger" (
                    "Id", "EventType", "AssetType", "ReferenceId",
                    "SenderAccountId", "SenderCharacterId", "SenderAccountCreatedUtc", "SenderCharacterLevel",
                    "AssetId", "AssetName", "Quantity", "UnitValue", "TotalValue", "Source", "OccurredAt")
                SELECT
                    md5(orders."Id"::text || ':fee')::uuid,
                    'MarketplaceFee', 'Currency', orders."Id",
                    orders."SellerAccountId", orders."SellerId", seller_user."CreatedUtc", seller."Level",
                    'currency:cinders', 'Cinders', orders."SellerFee", 1, orders."SellerFee",
                    'marketplace:fee:legacy-backfill', orders."PurchasedAt"
                FROM "MarketPlaceOrders" AS orders
                LEFT JOIN "Entities" AS seller ON seller."Id" = orders."SellerId"
                LEFT JOIN "Users" AS seller_user ON seller_user."Id" = orders."SellerAccountId"
                WHERE orders."SellerFee" > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_BuyerAccountId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "BuyerAccountId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_SellerAccountId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "SellerAccountId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstances_AcquiredAtUtc",
                table: "ItemInstances",
                column: "AcquiredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstances_AcquisitionSource",
                table: "ItemInstances",
                column: "AcquisitionSource");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_AssetId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "AssetId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_DestinationItemInstanceId",
                table: "EconomyLedger",
                column: "DestinationItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_EventType_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_GuildId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "GuildId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_OccurredAt",
                table: "EconomyLedger",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_RecipientAccountId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "RecipientAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_RecipientCharacterId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "RecipientCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_ReferenceId",
                table: "EconomyLedger",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_SenderAccountId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "SenderAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_SenderCharacterId_OccurredAt",
                table: "EconomyLedger",
                columns: new[] { "SenderCharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyLedger_SourceItemInstanceId",
                table: "EconomyLedger",
                column: "SourceItemInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EconomyLedger");

            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceOrders_BuyerAccountId_PurchasedAt",
                table: "MarketPlaceOrders");

            migrationBuilder.DropIndex(
                name: "IX_MarketPlaceOrders_SellerAccountId_PurchasedAt",
                table: "MarketPlaceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ItemInstances_AcquiredAtUtc",
                table: "ItemInstances");

            migrationBuilder.DropIndex(
                name: "IX_ItemInstances_AcquisitionSource",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "BuyerAccountId",
                table: "MarketPlaceOrders");

            migrationBuilder.DropColumn(
                name: "SellerAccountId",
                table: "MarketPlaceOrders");

            migrationBuilder.DropColumn(
                name: "AcquiredAtUtc",
                table: "ItemInstances");

            migrationBuilder.DropColumn(
                name: "AcquisitionSource",
                table: "ItemInstances");
        }
    }
}
