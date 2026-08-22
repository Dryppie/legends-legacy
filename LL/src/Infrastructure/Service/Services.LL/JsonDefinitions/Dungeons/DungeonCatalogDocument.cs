using System.Text.Json.Serialization;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Items;

namespace Services.LL.JsonDefinitions.Dungeons;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DungeonCatalogDocument
{
    public int SchemaVersion { get; set; }
    public List<DungeonFamilyDefinition> Families { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DungeonFamilyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SigilItemId { get; set; } = string.Empty;
    public int Region { get; set; } = 1;
    public string? RequiredAreaId { get; set; }
    public string? RequiredQuestId { get; set; }
    public List<DungeonEntryCost> EntryCosts { get; set; } = [];
    public Dictionary<ItemType, double> MonsterLootModifiers { get; set; } = [];
    public List<string> GatheringBonusRewardTableIds { get; set; } = [];
    public int RestSiteCount { get; set; } = -1;
    public List<DungeonRoomTemplateDefinition> RoomTemplates { get; set; } = [];
    public List<DungeonDifficultyDefinition> Difficulties { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DungeonDifficultyDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public float? EnemyStrengthMultiplier { get; set; }
    public DungeonRewardTable RewardTable { get; set; } = new();
    public List<DungeonGatheringNodeDefinition> GatheringNodes { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DungeonRoomTemplateDefinition
{
    public string Id { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public List<string> EncounterIds { get; set; } = [];
    public string? FeaturedEncounterId { get; set; }
    public float Weight { get; set; } = 1.0f;
}
