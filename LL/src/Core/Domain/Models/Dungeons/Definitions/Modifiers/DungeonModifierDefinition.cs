namespace Domain.Models.Dungeons.Definitions.Modifiers;

public sealed class DungeonModifierDefinition
{
    public Guid Id { get; init; }
    public string Key { get; init; } = default!;          // "healing_reduced_40"
    public ModifierScope Scope { get; init; }

    // Your combat engine/effect system reads these.
    public IReadOnlyDictionary<string, string> Params { get; init; } = new Dictionary<string, string>();
}
