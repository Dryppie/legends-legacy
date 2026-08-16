namespace Persistence.LL.Migrations;

internal static class EquipmentV17MigrationSql
{
    private const string PercentageAttributes = "4,5,6,7,8,9,10,11,13,14,15,16,19";

    public static IReadOnlyList<string> UpStatements { get; } =
    [
        CreateV15ToV16Sql(
            "InstanceAttributeModifier",
            "ItemInstances",
            "ItemInstanceId",
            "Id",
            "parent.\"ItemType\" = 0"),
        CreateV15ToV16Sql(
            "EquipmentAttributeModifierSnapshot",
            "EquipmentSnapshot",
            "EquipmentSnapshotId",
            "Id",
            "TRUE"),
        CreateV16ToV17Sql(
            "InstanceAttributeModifier",
            "ItemInstances",
            "ItemInstanceId",
            "Id",
            "parent.\"ItemType\" = 0"),
        CreateV16ToV17Sql(
            "EquipmentAttributeModifierSnapshot",
            "EquipmentSnapshot",
            "EquipmentSnapshotId",
            "Id",
            "TRUE")
    ];

    private static string CreateV15ToV16Sql(
        string modifierTable,
        string parentTable,
        string foreignKey,
        string parentKey,
        string parentPredicate) =>
        $"""
        UPDATE "{modifierTable}" AS modifier
        SET "Amount" = ROUND((modifier."Amount"::double precision
            * CASE modifier."AttributeType"
                WHEN 0 THEN CASE WHEN parent."Tier" <= 5
                    THEN 24.0 + (12.0 - 24.0) * (GREATEST(parent."Tier", 1) - 1) / 4.0
                    ELSE 12.0 + (18.0 - 12.0) * (LEAST(parent."Tier", 10) - 5) / 5.0 END
                WHEN 2 THEN CASE WHEN parent."Tier" <= 5
                    THEN 0.68 + (1.87 - 0.68) * (GREATEST(parent."Tier", 1) - 1) / 4.0
                    ELSE 1.87 + (4.12 - 1.87) * (LEAST(parent."Tier", 10) - 5) / 5.0 END
                WHEN 3 THEN CASE WHEN parent."Tier" <= 5
                    THEN 0.68 + (1.87 - 0.68) * (GREATEST(parent."Tier", 1) - 1) / 4.0
                    ELSE 1.87 + (4.12 - 1.87) * (LEAST(parent."Tier", 10) - 5) / 5.0 END
                WHEN 5 THEN 2.0 + (2.5 - 2.0) * (LEAST(GREATEST(parent."Tier", 1), 10) - 1) / 9.0
                WHEN 12 THEN CASE WHEN parent."Tier" <= 5 THEN 1.5
                    ELSE 1.5 + (2.1 - 1.5) * (LEAST(parent."Tier", 10) - 5) / 5.0 END
                WHEN 15 THEN CASE WHEN parent."Tier" <= 5 THEN 2.0
                    ELSE 2.0 + (2.2 - 2.0) * (LEAST(parent."Tier", 10) - 5) / 5.0 END
                WHEN 1 THEN 0.2 WHEN 4 THEN 4.0 WHEN 6 THEN 3.0 WHEN 7 THEN 3.0
                WHEN 8 THEN 5.0 WHEN 9 THEN 5.0 WHEN 10 THEN 6.0 WHEN 11 THEN 3.0
                WHEN 13 THEN 6.0 WHEN 14 THEN 6.0 WHEN 16 THEN 2.0 WHEN 19 THEN 2.8
            END
            / CASE modifier."AttributeType"
                WHEN 0 THEN 24.0 WHEN 1 THEN 0.2 WHEN 2 THEN 0.68 WHEN 3 THEN 0.68
                WHEN 4 THEN 4.0 WHEN 5 THEN 2.0 WHEN 6 THEN 3.0 WHEN 7 THEN 3.0
                WHEN 8 THEN 5.0 WHEN 9 THEN 5.0 WHEN 10 THEN 6.0 WHEN 11 THEN 3.0
                WHEN 12 THEN 1.5 WHEN 13 THEN 6.0 WHEN 14 THEN 6.0 WHEN 15 THEN 2.0
                WHEN 16 THEN 2.0 WHEN 19 THEN 2.8
            END)::numeric, 2)::real
        FROM "{parentTable}" AS parent
        WHERE modifier."{foreignKey}" = parent."{parentKey}"
          AND {parentPredicate}
          AND parent."BaseRecipeId" IS NOT NULL
          AND parent."StatModelVersion" < 16
          AND modifier."ModifierType" = 0
          AND modifier."Amount" > 0
          AND modifier."AttributeType" IN (0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,19);

        UPDATE "{parentTable}" AS parent
        SET "StatModelVersion" = 16
        WHERE {parentPredicate} AND parent."BaseRecipeId" IS NOT NULL AND parent."StatModelVersion" < 16;
        """;

    private static string CreateV16ToV17Sql(
        string modifierTable,
        string parentTable,
        string foreignKey,
        string parentKey,
        string parentPredicate) =>
        $"""
        WITH ranked AS (
            SELECT modifier."Id", parent."Tier", modifier."AttributeType",
                   SUM(GREATEST(modifier."Amount", 0)) OVER (
                       PARTITION BY modifier."{foreignKey}", modifier."AttributeType") AS raw_total,
                   ROW_NUMBER() OVER (
                       PARTITION BY modifier."{foreignKey}", modifier."AttributeType"
                       ORDER BY modifier."Id") AS row_number
            FROM "{modifierTable}" AS modifier
            JOIN "{parentTable}" AS parent ON parent."{parentKey}" = modifier."{foreignKey}"
            WHERE {parentPredicate}
              AND parent."BaseRecipeId" IS NOT NULL
              AND parent."StatModelVersion" = 16
              AND modifier."ModifierType" = 0
              AND modifier."AttributeType" IN ({PercentageAttributes})
        ), normalized AS (
            SELECT *, raw_total / POWER(15.2::double precision,
                (GREATEST("Tier", 1) - 1) / 9.0) AS value
            FROM ranked WHERE row_number = 1
        )
        UPDATE "{modifierTable}" AS modifier
        SET "Amount" = ROUND((CASE normalized."AttributeType"
            WHEN 4 THEN 100.0 * value / (100.0 + value)
            WHEN 5 THEN 300.0 * value / (300.0 + value)
            WHEN 6 THEN 60.0 * value / (60.0 + value)
            WHEN 7 THEN 60.0 * value / (60.0 + value)
            WHEN 8 THEN 50.0 * value / (50.0 + value)
            WHEN 9 THEN 50.0 * value / (50.0 + value)
            WHEN 10 THEN 40.0 * value / (40.0 + value)
            WHEN 11 THEN 300.0 * value / (300.0 + value)
            WHEN 13 THEN 50.0 * value / (50.0 + value)
            WHEN 14 THEN 100.0 * (1.0 - 1.0 / (1.0 + value / 160.0))
            WHEN 15 THEN 80.0 * value / (20.0 + value)
            WHEN 16 THEN 80.0 * value / (20.0 + value)
            WHEN 19 THEN 300.0 * value / (300.0 + value)
        END)::numeric, 2)::real
        FROM normalized WHERE modifier."Id" = normalized."Id";

        WITH duplicates AS (
            SELECT modifier."Id", ROW_NUMBER() OVER (
                PARTITION BY modifier."{foreignKey}", modifier."AttributeType"
                ORDER BY modifier."Id") AS row_number
            FROM "{modifierTable}" AS modifier
            JOIN "{parentTable}" AS parent ON parent."{parentKey}" = modifier."{foreignKey}"
            WHERE {parentPredicate}
              AND parent."BaseRecipeId" IS NOT NULL
              AND parent."StatModelVersion" = 16
              AND modifier."ModifierType" = 0
              AND modifier."AttributeType" IN ({PercentageAttributes})
        )
        DELETE FROM "{modifierTable}" AS modifier
        USING duplicates
        WHERE modifier."Id" = duplicates."Id" AND duplicates.row_number > 1;

        UPDATE "{parentTable}" AS parent
        SET "StatModelVersion" = 17
        WHERE {parentPredicate} AND parent."BaseRecipeId" IS NOT NULL AND parent."StatModelVersion" = 16;
        """;
}
