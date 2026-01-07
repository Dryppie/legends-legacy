using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Floors;
using Domain.Models.Dungeons.Definitions.Modifiers;

namespace Domain.Models.Dungeons;

public sealed class DungeonDefinition
{
    public Guid Id { get; init; }
    public string Key { get; init; } = default!;         // e.g. "crypt_of_thorns"
    public string Name { get; init; } = default!;
    public DungeonMode Mode { get; init; }
    public List<FloorDefinition> Floors { get; init; } = [];

    // Baseline modifiers always applied for this dungeon.
    public List<DungeonModifierDefinition> BaseModifiers { get; init; } = [];

    // Determines event rolls on Event floors.
    public EventTableDefinition EventTable { get; init; } = new();

    // Boss & miniboss identities are explicit (no RNG here unless you want variants).
    public Guid MiniBossEncounterId { get; init; }
    public Guid BossEncounterId { get; init; }

    // Optional: requirements / gating (keys, level, unlocks etc.)
    public bool RequiresKey { get; init; }
    public string? KeyItemId { get; init; }
    public int? RecommendedPower { get; init; }
}
