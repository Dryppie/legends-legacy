using Domain.Models.Inventories;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record DungeonEncounterCalculatedOutcome(
    Guid EncounterId,
    int ExperienceGained,
    int CindersGained,
    IReadOnlyList<InventoryItem> Loot);
