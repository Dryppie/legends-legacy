namespace Domain.Models.Dungeons.Definitions.Modifiers;

public sealed class DungeonBlessingDefinition
{
    public Guid Id { get; init; }
    public string Key { get; init; } = default!;
    public IReadOnlyDictionary<string, string> Params { get; init; } = new Dictionary<string, string>();
}
