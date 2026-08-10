using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildVaultAndRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildRolePermissions",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CanInvite = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageApplications = table.Column<bool>(type: "boolean", nullable: false),
                    CanPromoteDemote = table.Column<bool>(type: "boolean", nullable: false),
                    CanKick = table.Column<bool>(type: "boolean", nullable: false),
                    CanBorrowVault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRolePermissions", x => new { x.GuildId, x.Role });
                    table.ForeignKey(
                        name: "FK_GuildRolePermissions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "GuildRolePermissions"
                    ("GuildId", "Role", "CanInvite", "CanManageApplications", "CanPromoteDemote", "CanKick", "CanBorrowVault")
                SELECT "Id", 0, TRUE, TRUE, TRUE, TRUE, TRUE FROM "Guilds"
                UNION ALL
                SELECT "Id", 1, TRUE, TRUE, FALSE, FALSE, TRUE FROM "Guilds"
                UNION ALL
                SELECT "Id", 2, FALSE, FALSE, FALSE, FALSE, TRUE FROM "Guilds"
                ON CONFLICT ("GuildId", "Role") DO NOTHING;
                """);

            migrationBuilder.CreateTable(
                name: "GuildVaultItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DonatedByCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DonatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BorrowedByCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    BorrowedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildVaultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildVaultItems_Entities_BorrowedByCharacterId",
                        column: x => x.BorrowedByCharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GuildVaultItems_Entities_DonatedByCharacterId",
                        column: x => x.DonatedByCharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildVaultItems_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildVaultItems_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildVaultItems_BorrowedByCharacterId",
                table: "GuildVaultItems",
                column: "BorrowedByCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildVaultItems_DonatedByCharacterId",
                table: "GuildVaultItems",
                column: "DonatedByCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildVaultItems_EquipmentInstanceId",
                table: "GuildVaultItems",
                column: "EquipmentInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildVaultItems_GuildId_BorrowedByCharacterId",
                table: "GuildVaultItems",
                columns: new[] { "GuildId", "BorrowedByCharacterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildRolePermissions");

            migrationBuilder.DropTable(
                name: "GuildVaultItems");
        }
    }
}
