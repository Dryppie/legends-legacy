namespace Domain.Models.Dungeons.Mastery;

public sealed record DungeonMasteryBenefitSet(
    int AdditionalVisibilityRows,
    int RestSiteVigorBonus,
    int CombatVigorCostReduction,
    int CompletionCurrencyBonusPercent,
    int EquipmentDropChanceBonusPercentagePoints);

public sealed record DungeonMasteryBenefitDefinition(
    int Level,
    string Id,
    string Name,
    string Description);

public static class DungeonMasteryBenefits
{
    public const int MaxLevel = 10;
    public const int EquipmentDropChanceBonusPerLevel = 5;

    public static IReadOnlyList<DungeonMasteryBenefitDefinition> Definitions { get; } =
    new DungeonMasteryBenefitDefinition[]
    {
        new(1, "dungeon_sense_i", "Dungeon Sense I", "+1 visibility row (2 rows ahead)."),
        new(2, "campcraft_i", "Campcraft I", "Rest Sites restore +2 Vigor."),
        new(3, "equipment_discovery_i", "Equipment Discovery I", ""),
        new(4, "sure_footed_i", "Sure-Footed I", "Combat costs 1 less Vigor."),
        new(5, "familiar_spoils", "Familiar Spoils", "+10% Cinders and Soulstones from completion rewards."),
        new(6, "dungeon_sense_ii", "Dungeon Sense II", "+1 visibility row (3 rows ahead in total)."),
        new(7, "campcraft_ii", "Campcraft II", "Rest Sites restore another +2 Vigor."),
        new(8, "equipment_discovery_ii", "Equipment Discovery II", ""),
        new(9, "sure_footed_ii", "Sure-Footed II", "Combat costs another 1 less Vigor."),
        new(10, "soulstone_mastery", "Soulstone Mastery", "Reach Mastery 10 to receive 50 / 100 / 200 Soulstones once per dungeon family, based on dungeon tier.")
    }.Select(benefit => benefit with
    {
        Description = $"{benefit.Description} +{EquipmentDropChanceBonusPerLevel} percentage points to the equipment drop chance.".Trim()
    }).ToArray();

    public static DungeonMasteryBenefitSet Resolve(int level)
    {
        level = Math.Clamp(level, 0, MaxLevel);

        return new DungeonMasteryBenefitSet(
            AdditionalVisibilityRows: (level >= 1 ? 1 : 0) + (level >= 6 ? 1 : 0),
            RestSiteVigorBonus: (level >= 2 ? 2 : 0) + (level >= 7 ? 2 : 0),
            CombatVigorCostReduction: (level >= 4 ? 1 : 0) + (level >= 9 ? 1 : 0),
            CompletionCurrencyBonusPercent: level >= 5 ? 10 : 0,
            EquipmentDropChanceBonusPercentagePoints: level * EquipmentDropChanceBonusPerLevel);
    }
}
