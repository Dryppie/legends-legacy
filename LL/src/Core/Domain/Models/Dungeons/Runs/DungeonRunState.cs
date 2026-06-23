using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;

namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonRunState
{
    public Guid RunId { get; set; }
    public string MechanicId { get; set; } = "pressure";
    public string MechanicDisplayName { get; set; } = "Pressure";
    public int MechanicMaxValue { get; set; } = 100;
    public int Pressure { get; set; }
    public int RewardMultiplierPercent { get; set; } = 100;
    public List<string> ActiveBoonIds { get; set; } = [];
    public List<DungeonActiveBoonSummary> ActiveBoonSummaries { get; set; } = [];
    public List<DungeonBoonEffectSummary> ActiveBoonEffectSummaries { get; set; } = [];
    public Dictionary<string, int> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DungeonLootBag SecuredLoot { get; set; } = new();
    public DungeonLootBag UnsecuredLoot { get; set; } = new();
    public List<DungeonRouteOption> CurrentRouteOptions { get; set; } = [];
    public List<DungeonEventChoiceOption> CurrentEventChoices { get; set; } = [];
    public List<DungeonCheckpointChoiceOption> CurrentCheckpointChoices { get; set; } = [];
    public List<DungeonBoonChoiceOption> CurrentBoonChoices { get; set; } = [];
    public List<DungeonBossModifier> CurrentBossModifiers { get; set; } = [];
    public List<DungeonMechanicThresholdState> CurrentMechanicThresholds { get; set; } = [];
    public List<DungeonMasteryAwardReason> MasteryAwardReasons { get; set; } = [];
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
    public int PressureDelta { get; set; }
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
    public int PressureDelta { get; set; }
    public int RewardMultiplierDeltaPercent { get; set; }
    public List<string> AddFlags { get; set; } = [];
    public List<string> RemoveFlags { get; set; } = [];
    public List<string> MissingRequirements { get; set; } = [];
    public bool GrantsBoonChoice { get; set; }
    public bool GrantsLoot { get; set; }
    public int AmbushChancePercent { get; set; }
    public bool RevealsHiddenRoute { get; set; }
}

public sealed class DungeonCheckpointChoiceOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PressureDelta { get; set; }
    public int RewardMultiplierDeltaPercent { get; set; }
}

public sealed class DungeonBoonChoiceOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rarity { get; set; } = "Common";
    public List<string> EffectSummaries { get; set; } = [];
}

public sealed class DungeonActiveBoonSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rarity { get; set; } = "Common";
    public int Count { get; set; } = 1;
    public List<string> EffectSummaries { get; set; } = [];
}

public sealed class DungeonBoonEffectSummary
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
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

public sealed class DungeonMechanicThresholdState
{
    public string Id { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public int RewardMultiplierBonusPercent { get; set; }
}

public sealed class DungeonPressureResult
{
    public int PreviousPressure { get; init; }
    public int Pressure { get; init; }
    public int RewardMultiplierPercent { get; init; }
    public IReadOnlyList<string> ActiveThresholdIds { get; init; } = [];
}
