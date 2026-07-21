namespace Domain.Models.Dungeons.Mastery;

public sealed record DungeonMasteryBenefitSet(
    int AdditionalVisibilityRows,
    int RestSiteVigorBonus,
    double GatheringProcChanceBonus,
    int CombatVigorCostReduction,
    int CompletionCurrencyBonusPercent);

public sealed record DungeonMasteryBenefitDefinition(
    int Level,
    string Id,
    string Name,
    string Description);

public static class DungeonMasteryBenefits
{
    public const int MaxLevel = 10;

    public static IReadOnlyList<DungeonMasteryBenefitDefinition> Definitions { get; } =
    [
        new(1, "dungeon_sense_i", "Dungeon Sense I", "+1 visibility row (2 rows ahead)."),
        new(2, "campcraft_i", "Campcraft I", "Rest Sites restore +2 Vigor."),
        new(3, "dungeon_forager_i", "Dungeon Forager I", "+5 percentage points to dungeon gathering chance."),
        new(4, "sure_footed_i", "Sure-Footed I", "Combat costs 1 less Vigor."),
        new(5, "familiar_spoils", "Familiar Spoils", "+10% Cinders and Soulstones from completion rewards."),
        new(6, "dungeon_sense_ii", "Dungeon Sense II", "+1 visibility row (3 rows ahead in total)."),
        new(7, "campcraft_ii", "Campcraft II", "Rest Sites restore another +2 Vigor."),
        new(8, "dungeon_forager_ii", "Dungeon Forager II", "Another +5 percentage points to dungeon gathering chance."),
        new(9, "sure_footed_ii", "Sure-Footed II", "Combat costs another 1 less Vigor."),
        new(10, "soulstone_mastery", "Soulstone Mastery", "Reach Mastery 10 to receive 50 / 100 / 200 Soulstones based on difficulty.")
    ];

    public static DungeonMasteryBenefitSet Resolve(int level)
    {
        level = Math.Clamp(level, 0, MaxLevel);

        return new DungeonMasteryBenefitSet(
            AdditionalVisibilityRows: (level >= 1 ? 1 : 0) + (level >= 6 ? 1 : 0),
            RestSiteVigorBonus: (level >= 2 ? 2 : 0) + (level >= 7 ? 2 : 0),
            GatheringProcChanceBonus: (level >= 3 ? 0.05d : 0d) + (level >= 8 ? 0.05d : 0d),
            CombatVigorCostReduction: (level >= 4 ? 1 : 0) + (level >= 9 ? 1 : 0),
            CompletionCurrencyBonusPercent: level >= 5 ? 10 : 0);
    }
}
