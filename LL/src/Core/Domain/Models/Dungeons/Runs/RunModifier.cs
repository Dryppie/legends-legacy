namespace Domain.Models.Dungeons.Runs;

public sealed class RunModifier
{
    public Guid ModifierDefinitionId { get; set; }
    public string Key { get; set; } = default!;
    public Dictionary<string, string> Params { get; set; } = new();

    // Optional timeboxing
    public int? ExpiresAfterFloorIndex { get; set; }
}