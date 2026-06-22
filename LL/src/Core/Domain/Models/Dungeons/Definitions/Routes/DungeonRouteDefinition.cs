using Domain.Models.Dungeons.Definitions.Rooms;

namespace Domain.Models.Dungeons.Definitions.Routes;

public sealed class DungeonRouteDefinition
{
    public string Id { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
    public int PressureDelta { get; set; }
    public bool IsUnknown { get; set; }
    public List<string> DungeonDefinitionIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> PossibleRewards { get; set; } = [];
    public List<string> Requirements { get; set; } = [];
}
