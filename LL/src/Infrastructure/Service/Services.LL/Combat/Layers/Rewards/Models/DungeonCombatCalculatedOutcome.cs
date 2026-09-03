using Domain.Models.Combat;
using Domain.Models.Inventories;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record DungeonCombatCalculatedOutcome(
    Guid CharacterId,
    int TotalExperience,
    int TotalCinders,
    int TotalSoulstones,
    IReadOnlyList<InventoryItem> TotalLoot,
    IReadOnlyList<DungeonEncounterCalculatedOutcome> EncounterOutcomes)
{
    public DungeonEncounterCalculatedOutcome? LastEncounterOutcome =>
        EncounterOutcomes.Count == 0 ? null : EncounterOutcomes[^1];
}
