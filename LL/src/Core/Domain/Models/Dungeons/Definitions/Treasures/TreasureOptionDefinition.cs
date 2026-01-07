namespace Domain.Models.Dungeons.Definitions.Treasures;

public sealed class TreasureOptionDefinition
{
    public TreasureOptionType Type { get; init; }
    public string Title { get; init; } = default!;
    public IReadOnlyDictionary<string, string> Params { get; init; } = new Dictionary<string, string>();
}