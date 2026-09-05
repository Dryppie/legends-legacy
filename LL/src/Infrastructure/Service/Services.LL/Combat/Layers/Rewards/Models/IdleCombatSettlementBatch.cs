using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record IdleCombatSettlementBatch(
    Guid CharacterId,
    DateTimeOffset From,
    DateTimeOffset ProcessedUntil,
    string AreaId,
    string AreaName,
    IReadOnlyList<InventoryItem> Loot,
    int Cinders,
    int Soulstones,
    IReadOnlyList<Creature> DefeatedCreatures,
    IReadOnlyList<string> DefeatedCreatureFamilyKeys,
    int PlayerDefeats,
    int? LowestWinningHealthPercent,
    int ActionCount,
    int WinningEncounterCount,
    IReadOnlyList<ProphecyProgressEvent> ProphecyProgressEvents);
