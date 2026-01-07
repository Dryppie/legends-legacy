using Domain.Models.Dungeons.Definitions.Modifiers;

namespace Domain.Models.Dungeons.Definitions.Encounters;

public sealed class EncounterDefinition
{
    public Guid Id { get; init; }
    public EncounterKind Kind { get; init; }

    public string Name { get; init; } = default!;
    public int DifficultyRating { get; init; }            // arbitrary scale for balancing
    public List<string> MonsterIds { get; init; } = [];

    // Optional: per-encounter modifiers (e.g. “boss reflects crits”)
    public List<DungeonModifierDefinition> Modifiers { get; init; } = [];

    //public LootTableDefinition Loot { get; init; } = new();
}
