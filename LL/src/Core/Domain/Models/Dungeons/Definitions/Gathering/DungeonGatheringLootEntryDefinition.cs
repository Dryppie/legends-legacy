namespace Domain.Models.Dungeons.Definitions.Gathering;

public sealed class DungeonGatheringLootEntryDefinition
{
    public string ItemId { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
    public bool IsRare { get; set; }
}
