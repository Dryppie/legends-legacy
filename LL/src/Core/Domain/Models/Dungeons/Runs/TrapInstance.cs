namespace Domain.Models.Dungeons.Runs;

public sealed class TrapInstance
{
    public string TrapKey { get; set; } = default!;
    public Dictionary<string, string> Params { get; set; } = new();
    public bool Resolved { get; set; }
}