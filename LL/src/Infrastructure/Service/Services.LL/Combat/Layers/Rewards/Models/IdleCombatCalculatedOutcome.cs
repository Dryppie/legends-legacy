using Domain.Models.Inventories;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record IdleCombatCalculatedOutcome(
    Guid CharacterId,
    DateTimeOffset From,
    DateTimeOffset ProcessedUntil,
    int TotalExperience,
    int TotalCinders,
    int TotalSoulstones,
    IReadOnlyList<InventoryItem> TotalLoot,
    IReadOnlyList<IdleEncounterCalculatedOutcome> EncounterOutcomes)
{
    public IdleEncounterCalculatedOutcome? LastEncounterOutcome =>
        EncounterOutcomes.Count == 0 ? null : EncounterOutcomes[^1];
}