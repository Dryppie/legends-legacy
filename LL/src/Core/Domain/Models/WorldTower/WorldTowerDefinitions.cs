namespace Domain.Models.WorldTower;

public sealed class TowerFloorDefinition
{
    public int FloorNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public TowerFloorType Type { get; init; }
    public Guid GuardianCreatureId { get; init; }
    public string GuardianName { get; init; } = string.Empty;
    public string GuardianImagePath { get; init; } = string.Empty;
    public IReadOnlyList<string> GuardianTags { get; init; } = [];
    public int RequiredSlots { get; init; }
    public int RecommendedPowerRating { get; init; }
    public float GuardianStrengthMultiplier { get; init; } = 1;
    public bool EchoEnabledAfterClear { get; init; }
    public int FirstClearCinders { get; init; }
    public int EchoCinders { get; init; }
    public IReadOnlyList<string> UnlockKeys { get; init; } = [];
    public IReadOnlyList<TowerScoutingRevealDefinition> ScoutingReveals { get; init; } = [];
}

public sealed class TowerScoutingRevealDefinition
{
    public int Threshold { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class WorldTowerCatalogDocument
{
    public IReadOnlyList<TowerFloorDefinition> Floors { get; init; } = [];
}
