using Domain.Models.Dungeons.Definitions.Treasures;

namespace Domain.Models.Dungeons.Runs;

public sealed class TreasureOptionInstance
{
    public TreasureOptionType Type { get; set; }
    public string Title { get; set; } = default!;
    public Dictionary<string, string> Params { get; set; } = new();
}
