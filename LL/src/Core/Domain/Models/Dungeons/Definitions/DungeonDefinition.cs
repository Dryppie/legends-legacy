using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Items;

namespace Domain.Models.Dungeons;

public sealed class DungeonDefinition
{
    public string Id { get; set; } = default!;         // e.g. "crypt_of_thorns"
    public string Name { get; set; } = default!;
    public string SigilItemId { get; set; } = default!;
    public int Region { get; set; } = 1;
    public DungeonGrade Grade { get; set; } = DungeonGrade.GradeI;
    public int Tier { get; set; } = 1;
    public float? EnemyStrengthMultiplier { get; set; }
    public string? RequiredAreaId { get; set; }
    public string? RequiredQuestId { get; set; }
    public string? RequiredPreviousDungeonId { get; set; }
    public DungeonGrade? RequiredPreviousDungeonGrade { get; set; }
    public List<DungeonEntryCost> EntryCosts { get; set; } = [];
    public DungeonRewardTable RewardTable { get; set; } = new();
    public List<string> CompletionRewardTableIds { get; set; } = [];
    public List<string> TierRewardTableIds { get; set; } = [];
    public Dictionary<ItemType, double> MonsterLootModifiers { get; set; } = [];
    public List<DungeonGatheringNodeDefinition> GatheringNodes { get; set; } = [];
    public int RestSiteCount { get; set; }
    public int MinRooms { get; set; }
    public int MaxRooms { get; set; }
    public List<RoomDefinition> Rooms { get; set; } = [];
}
