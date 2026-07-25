using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class SeparateEquipmentSnapshotModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentAttributeModifierSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAttributeModifierSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAttributeModifierSnapshot_EquipmentSnapshot_Equipm~",
                        column: x => x.EquipmentSnapshotId,
                        principalTable: "EquipmentSnapshot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAttributeModifierSnapshot_EquipmentSnapshotId",
                table: "EquipmentAttributeModifierSnapshot",
                column: "EquipmentSnapshotId");

            migrationBuilder.Sql(
                """
                INSERT INTO "EquipmentAttributeModifierSnapshot"
                    ("Id", "EquipmentSnapshotId", "AttributeType", "Amount", "ModifierType")
                SELECT
                    "Id", "EquipmentSnapshotId", "AttributeType", "Amount", "ModifierType"
                FROM "InstanceAttributeModifier"
                WHERE "EquipmentSnapshotId" IS NOT NULL;
                """);

            // Snapshot clones also point at the live item through ItemInstanceId. Moving and
            // deleting these rows removes the duplicated stats without touching real modifiers.
            migrationBuilder.Sql(
                """
                DELETE FROM "InstanceAttributeModifier"
                WHERE "EquipmentSnapshotId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_InstanceAttributeModifier_EquipmentSnapshot_EquipmentSnapsh~",
                table: "InstanceAttributeModifier");

            migrationBuilder.DropIndex(
                name: "IX_InstanceAttributeModifier_EquipmentSnapshotId",
                table: "InstanceAttributeModifier");

            migrationBuilder.DropColumn(
                name: "EquipmentSnapshotId",
                table: "InstanceAttributeModifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentSnapshotId",
                table: "InstanceAttributeModifier",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanceAttributeModifier_EquipmentSnapshotId",
                table: "InstanceAttributeModifier",
                column: "EquipmentSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_InstanceAttributeModifier_EquipmentSnapshot_EquipmentSnapsh~",
                table: "InstanceAttributeModifier",
                column: "EquipmentSnapshotId",
                principalTable: "EquipmentSnapshot",
                principalColumn: "Id");

            migrationBuilder.Sql(
                """
                INSERT INTO "InstanceAttributeModifier"
                    ("Id", "ItemInstanceId", "EquipmentSnapshotId", "AttributeType", "Amount", "ModifierType")
                SELECT
                    modifier."Id",
                    snapshot."EquipmentInstanceId",
                    modifier."EquipmentSnapshotId",
                    modifier."AttributeType",
                    modifier."Amount",
                    modifier."ModifierType"
                FROM "EquipmentAttributeModifierSnapshot" modifier
                INNER JOIN "EquipmentSnapshot" snapshot
                    ON snapshot."Id" = modifier."EquipmentSnapshotId"
                INNER JOIN "ItemInstances" item
                    ON item."Id" = snapshot."EquipmentInstanceId";
                """);

            migrationBuilder.DropTable(
                name: "EquipmentAttributeModifierSnapshot");
        }
    }
}
