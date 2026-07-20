using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;

namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonRunState
{
    public Guid RunId { get; set; }
    public Dictionary<string, int> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DungeonLootBag SecuredLoot { get; set; } = new();
    public DungeonLootBag PendingLoot { get; set; } = new();
    public List<DungeonMapNode> MapNodes { get; set; } = [];
    public List<int> TraversedRoomIndexes { get; set; } = [];
    public List<DungeonRouteOption> CurrentRouteOptions { get; set; } = [];
    public List<DungeonEventChoiceOption> CurrentEventChoices { get; set; } = [];
    public List<DungeonBossModifier> CurrentBossModifiers { get; set; } = [];
    public List<DungeonMasteryAwardReason> MasteryAwardReasons { get; set; } = [];
    public int Vigor { get; set; } = 100;
    public string VigorState { get; set; } = "Steady";
    public List<DungeonVigorThreshold> VigorThresholds { get; set; } = [];
    public int CurrentSection { get; set; } = 1;
    public int TotalSections { get; set; } = 1;
    public int RestSitesVisited { get; set; }
    public string LastConsequence { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public List<DungeonVigorChange> VigorHistory { get; set; } = [];
    public List<DungeonOmen> ActiveOmens { get; set; } = [];
    public List<DungeonBossAspect> BossAspects { get; set; } = [];
    public DungeonFailureAnalysis? FailureAnalysis { get; set; }
}

public sealed class DungeonMapNode
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int RoomIndex { get; set; }
    public int Depth { get; set; }
    public int Lane { get; set; }
    public int Section { get; set; }
    public string Forecast { get; set; } = string.Empty;
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
    public string BossConsequence { get; set; } = string.Empty;
    public string BossAspectId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<int> NextRoomIndexes { get; set; } = [];
}

public sealed class DungeonVigorChange
{
    public int RoomIndex { get; set; }
    public int Amount { get; set; }
    public int VigorAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DungeonVigorThreshold
{
    public string State { get; set; } = string.Empty;
    public int MinimumVigor { get; set; }
    public int MaximumVigor { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Effects { get; set; } = [];
    public bool IsCurrent { get; set; }
}

public sealed class DungeonOmen
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CombatTollModifier { get; set; }
    public int HazardTollModifier { get; set; }
}

public sealed class DungeonBossAspect
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string State { get; set; } = "Active";
    public string StateReason { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; } = ModifierType.Additive;
}

public sealed class DungeonFailureAnalysis
{
    public string Location { get; set; } = string.Empty;
    public int Section { get; set; }
    public string PrimaryCause { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = [];
    public DungeonLootBag LostPendingLoot { get; set; } = new();
}

public sealed class DungeonLootBag
{
    public int Experience { get; set; }
    public int Cinders { get; set; }
    public int Soulstones { get; set; }
    public Dictionary<string, int> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DungeonRouteOption
{
    public string Id { get; set; } = string.Empty;
    public int RoomIndex { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int RiskLevel { get; set; }
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
    public string Forecast { get; set; } = string.Empty;
    public string BossConsequence { get; set; } = string.Empty;
    public bool IsUnknown { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> PossibleRewards { get; set; } = [];
    public List<string> Requirements { get; set; } = [];
}

public sealed class DungeonEventChoiceOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VigorDelta { get; set; }
    public List<string> AddFlags { get; set; } = [];
    public List<string> RemoveFlags { get; set; } = [];
    public List<string> MissingRequirements { get; set; } = [];
    public bool GrantsLoot { get; set; }
    public int AmbushChancePercent { get; set; }
    public bool RevealsHiddenRoute { get; set; }
}

public sealed class DungeonBossModifier
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; } = ModifierType.Flat;
    public bool IsHelpfulToPlayer { get; set; }
}
