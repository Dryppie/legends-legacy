namespace Domain.Models.Dungeons.Definitions.Floors;

public sealed class FloorDefinition
{
    public int Index { get; init; }                       // 0..N-1
    public FloorType Type { get; init; }

    // For Combat floors: how many encounters (packs) on this floor
    public int MinEncounters { get; init; }
    public int MaxEncounters { get; init; }

    // Optional: additional modifiers that start on entering this floor
    public IReadOnlyList<DungeonModifierDefinition> Modifiers { get; init; } = Array.Empty<DungeonModifierDefinition>();
}