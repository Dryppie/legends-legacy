using Domain.Models.Dungeons.Definitions.Modifiers;

namespace Domain.Models.Dungeons.Definitions.Encounters;

public sealed class EncounterDefinition
{
    public Guid Id { get; init; }
    public EncounterKind Kind { get; init; }

    public string Name { get; init; } = default!;
    public int DifficultyRating { get; init; }            // arbitrary scale for balancing
    public IReadOnlyList<string> MonsterIds { get; init; } = Array.Empty<string>();

    // Optional: per-encounter modifiers (e.g. “boss reflects crits”)
    public IReadOnlyList<DungeonModifierDefinition> Modifiers { get; init; } = Array.Empty<DungeonModifierDefinition>();

    public LootTableDefinition Loot { get; init; } = new();
}
