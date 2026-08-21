namespace Domain.Models.WorldTower;

public sealed class TowerFloorDefinition
{
    public int FloorNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public TowerFloorType Type { get; init; }
    public Guid GuardianCreatureId { get; init; }
    public string GuardianName { get; init; } = string.Empty;
    public string GuardianAbilityProfileId { get; init; } = string.Empty;
    public int RequiredSlots { get; init; }
    public int RecommendedPowerRating { get; init; }
    public TowerGuardianScalingDefinition GuardianScaling { get; init; } = new();
    public bool EchoEnabledAfterClear { get; init; }
    public int TowerTokens { get; set; }
    public int FirstClearTowerTokens => checked(TowerTokens * 4);
    public IReadOnlyList<TowerUnlockDefinition> Unlocks { get; init; } = [];
}

public sealed class TowerUnlockDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class TowerGuardianScalingDefinition
{
    public float Health { get; init; } = 1;
    public float Offense { get; init; } = 1;
    public float Defense { get; init; } = 1;
    public float Resistance { get; init; } = 1;
    public float Penetration { get; init; } = 1;
    public float Regeneration { get; init; } = 1;
}

public sealed class WorldTowerCatalogDocument
{
    public int? ReleasedThroughFloor { get; init; }
    public TowerRewardCurveDefinition RewardCurve { get; init; } = new();
    public IReadOnlyList<TowerFloorDefinition> Floors { get; init; } = [];
}

public sealed class TowerRewardCurveDefinition
{
    public int BaseReward { get; init; } = 100;
    public int MaximumFloor { get; init; } = 100;
    public double MaximumMultiplier { get; init; } = 2.5;
    public double Exponent { get; init; } = 0.8;

    public int Calculate(int floorNumber)
    {
        if (floorNumber is < 1 || floorNumber > MaximumFloor)
            throw new ArgumentOutOfRangeException(nameof(floorNumber));

        var progress = MaximumFloor == 1
            ? 0d
            : (double)(floorNumber - 1) / (MaximumFloor - 1);
        var multiplier = 1d
                         + (MaximumMultiplier - 1d) * Math.Pow(progress, Exponent);
        return checked((int)Math.Round(
            BaseReward * multiplier,
            MidpointRounding.AwayFromZero));
    }
}
