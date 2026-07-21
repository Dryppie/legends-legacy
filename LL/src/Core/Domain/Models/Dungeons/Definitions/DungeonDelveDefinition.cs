using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonDelveDefinition
{
    public string Id { get; set; } = string.Empty;
    public List<string> DungeonDefinitionIds { get; set; } = [];
    public List<DungeonDelveNodeDefinition> Nodes { get; set; } = [];
}

public sealed class DungeonDelveNodeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int Depth { get; set; }
    public int Lane { get; set; }
    public int Section { get; set; }
    public List<int> NextRoomIndexes { get; set; } = [];
    public string Forecast { get; set; } = string.Empty;
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
}
