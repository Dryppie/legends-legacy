using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRunStateDto
{
    public int Pressure { get; set; }
    public string MechanicId { get; set; } = "pressure";
    public string MechanicDisplayName { get; set; } = "Pressure";
    public int MechanicMaxValue { get; set; } = 100;
    public int RewardMultiplierPercent { get; set; } = 100;
    public List<string> ActiveBoonIds { get; set; } = [];
    public List<DungeonActiveBoonSummaryDto> ActiveBoonSummaries { get; set; } = [];
    public List<DungeonBoonEffectSummaryDto> ActiveBoonEffectSummaries { get; set; } = [];
    public Dictionary<string, int> Flags { get; set; } = [];
    public DungeonLootBagDto SecuredLoot { get; set; } = new();
    public DungeonLootBagDto UnsecuredLoot { get; set; } = new();
    public List<DungeonRouteOptionDto> CurrentRouteOptions { get; set; } = [];
    public List<DungeonEventChoiceOptionDto> CurrentEventChoices { get; set; } = [];
    public List<DungeonCheckpointChoiceOptionDto> CurrentCheckpointChoices { get; set; } = [];
    public List<DungeonBoonChoiceOptionDto> CurrentBoonChoices { get; set; } = [];
    public List<DungeonBossModifierDto> CurrentBossModifiers { get; set; } = [];
    public List<DungeonMechanicThresholdStateDto> CurrentMechanicThresholds { get; set; } = [];
    public List<DungeonMasteryAwardReasonDto> MasteryAwardReasons { get; set; } = [];
}

public sealed class DungeonMasteryAwardReasonDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Experience { get; set; }
}

public sealed class DungeonLootBagDto
{
    public int Experience { get; set; }
    public int Cinders { get; set; }
    public int Soulstones { get; set; }
    public Dictionary<string, int> Items { get; set; } = [];
}

public sealed class DungeonRouteOptionDto
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

public sealed class DungeonEventChoiceOptionDto
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

public sealed class DungeonCheckpointChoiceOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PressureDelta { get; set; }
    public int RewardMultiplierDeltaPercent { get; set; }
}

public sealed class DungeonBoonChoiceOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public List<string> EffectSummaries { get; set; } = [];
}

public sealed class DungeonActiveBoonSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> EffectSummaries { get; set; } = [];
}

public sealed class DungeonBoonEffectSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public sealed class DungeonBossModifierDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string AttributeType { get; set; } = string.Empty;
    public float Amount { get; set; }
    public string ModifierType { get; set; } = string.Empty;
    public bool IsHelpfulToPlayer { get; set; }
}

public sealed class DungeonMechanicThresholdStateDto
{
    public string Id { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public int RewardMultiplierBonusPercent { get; set; }
}
