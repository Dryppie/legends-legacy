namespace Domain.Models.Dungeons;

public class DungeonAction
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Style { get; set; } = "primary";
    public bool Disabled { get; set; }
    public string? Description { get; set; }
}