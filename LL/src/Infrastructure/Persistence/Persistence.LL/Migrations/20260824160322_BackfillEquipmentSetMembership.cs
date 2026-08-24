using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class BackfillEquipmentSetMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ItemInstances"
                SET "EquipmentSetId" = CASE "BlueprintId"
                    WHEN 'blueprint_fury' THEN 'set_fury'
                    WHEN 'blueprint_arcane' THEN 'set_arcane'
                    WHEN 'blueprint_execution' THEN 'set_execution'
                    WHEN 'blueprint_aegis' THEN 'set_aegis'
                    WHEN 'blueprint_warden' THEN 'set_warden'
                    WHEN 'blueprint_endurance' THEN 'set_endurance'
                    WHEN 'blueprint_phoenix' THEN 'set_phoenix'
                    WHEN 'blueprint_spirit' THEN 'set_spirit'
                    WHEN 'blueprint_primal' THEN 'set_primal'
                    WHEN 'blueprint_venom' THEN 'set_venom'
                    WHEN 'blueprint_hive' THEN 'set_hive'
                END
                WHERE "BlueprintId" IN (
                    'blueprint_fury', 'blueprint_arcane', 'blueprint_execution',
                    'blueprint_aegis', 'blueprint_warden', 'blueprint_endurance',
                    'blueprint_phoenix', 'blueprint_spirit', 'blueprint_primal',
                    'blueprint_venom', 'blueprint_hive')
                  AND ("EquipmentSetId" IS NULL OR "EquipmentSetId" = '');

                UPDATE "EquipmentSnapshot"
                SET "EquipmentSetId" = CASE "BlueprintId"
                    WHEN 'blueprint_fury' THEN 'set_fury'
                    WHEN 'blueprint_arcane' THEN 'set_arcane'
                    WHEN 'blueprint_execution' THEN 'set_execution'
                    WHEN 'blueprint_aegis' THEN 'set_aegis'
                    WHEN 'blueprint_warden' THEN 'set_warden'
                    WHEN 'blueprint_endurance' THEN 'set_endurance'
                    WHEN 'blueprint_phoenix' THEN 'set_phoenix'
                    WHEN 'blueprint_spirit' THEN 'set_spirit'
                    WHEN 'blueprint_primal' THEN 'set_primal'
                    WHEN 'blueprint_venom' THEN 'set_venom'
                    WHEN 'blueprint_hive' THEN 'set_hive'
                END
                WHERE "BlueprintId" IN (
                    'blueprint_fury', 'blueprint_arcane', 'blueprint_execution',
                    'blueprint_aegis', 'blueprint_warden', 'blueprint_endurance',
                    'blueprint_phoenix', 'blueprint_spirit', 'blueprint_primal',
                    'blueprint_venom', 'blueprint_hive')
                  AND ("EquipmentSetId" IS NULL OR "EquipmentSetId" = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ItemInstances"
                SET "EquipmentSetId" = NULL
                WHERE ("BlueprintId", "EquipmentSetId") IN (
                    ('blueprint_fury', 'set_fury'),
                    ('blueprint_arcane', 'set_arcane'),
                    ('blueprint_execution', 'set_execution'),
                    ('blueprint_aegis', 'set_aegis'),
                    ('blueprint_warden', 'set_warden'),
                    ('blueprint_endurance', 'set_endurance'),
                    ('blueprint_phoenix', 'set_phoenix'),
                    ('blueprint_spirit', 'set_spirit'),
                    ('blueprint_primal', 'set_primal'),
                    ('blueprint_venom', 'set_venom'),
                    ('blueprint_hive', 'set_hive'));

                UPDATE "EquipmentSnapshot"
                SET "EquipmentSetId" = NULL
                WHERE ("BlueprintId", "EquipmentSetId") IN (
                    ('blueprint_fury', 'set_fury'),
                    ('blueprint_arcane', 'set_arcane'),
                    ('blueprint_execution', 'set_execution'),
                    ('blueprint_aegis', 'set_aegis'),
                    ('blueprint_warden', 'set_warden'),
                    ('blueprint_endurance', 'set_endurance'),
                    ('blueprint_phoenix', 'set_phoenix'),
                    ('blueprint_spirit', 'set_spirit'),
                    ('blueprint_primal', 'set_primal'),
                    ('blueprint_venom', 'set_venom'),
                    ('blueprint_hive', 'set_hive'));
                """);
        }
    }
}
