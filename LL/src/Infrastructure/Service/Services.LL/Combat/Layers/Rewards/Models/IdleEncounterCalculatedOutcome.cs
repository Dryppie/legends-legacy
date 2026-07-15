using Domain.Models.Inventories;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record IdleEncounterCalculatedOutcome(
    Guid EncounterId,
    int Sequence,
    int CreatureCount,
    int AreaBaseExperience,
    int BonusAdjustedExperience,
    int ExperienceGained,
    int CindersGained,
    IReadOnlyList<InventoryItem> Loot);
