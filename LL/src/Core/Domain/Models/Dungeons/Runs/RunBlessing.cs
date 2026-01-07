namespace Domain.Models.Dungeons.Runs;

public sealed class RunBlessing
{
    public Guid BlessingDefinitionId { get; set; }
    public string Key { get; set; } = default!;
    public Dictionary<string, string> Params { get; set; } = new();
}