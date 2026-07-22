namespace Domain.Models.Dungeons.Definitions.Rooms;

public sealed class RoomDefinition
{
    public RoomType Type { get; init; }
    public List<string> EncounterIds { get; set; } = [];
    public float Weight { get; init; } = 1.0f;
}
