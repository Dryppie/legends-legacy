using System.Text.Json;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.WorldTower;

namespace Services.LL.WorldTower;

public sealed class JsonWorldTowerDefinitionProvider : IWorldTowerDefinitionProvider
{
    private readonly IReadOnlyList<TowerFloorDefinition> _floors;
    private readonly IReadOnlyDictionary<int, TowerFloorDefinition> _byNumber;

    public JsonWorldTowerDefinitionProvider(string path, JsonSerializerOptions jsonOptions)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"World Tower catalog was not found at '{path}'.");
        }

        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<WorldTowerCatalogDocument>(stream, jsonOptions)
            ?? throw new InvalidOperationException("World Tower catalog is empty or invalid.");

        ValidateRewardCurve(document.RewardCurve);
        var authoredFloors = document.Floors.OrderBy(x => x.FloorNumber).ToArray();
        if (authoredFloors.Any(x => x.FloorNumber > document.RewardCurve.MaximumFloor))
        {
            throw new InvalidOperationException(
                "World Tower contains a floor beyond the configured reward curve maximum.");
        }
        foreach (var floor in authoredFloors)
            floor.TowerTokens = document.RewardCurve.Calculate(floor.FloorNumber);
        Validate(authoredFloors);

        var releasedThroughFloor = document.ReleasedThroughFloor ?? authoredFloors[^1].FloorNumber;
        if (releasedThroughFloor < 1 || authoredFloors.All(x => x.FloorNumber != releasedThroughFloor))
        {
            throw new InvalidOperationException(
                $"World Tower released-through floor {releasedThroughFloor} does not exist in the catalog.");
        }

        _floors = authoredFloors
            .Where(x => x.FloorNumber <= releasedThroughFloor)
            .ToArray();
        _byNumber = _floors.ToDictionary(x => x.FloorNumber);
    }

    public IReadOnlyList<TowerFloorDefinition> GetFloors() => _floors;

    public TowerFloorDefinition? GetFloor(int floorNumber) =>
        _byNumber.GetValueOrDefault(floorNumber);

    private static void Validate(IReadOnlyList<TowerFloorDefinition> floors)
    {
        if (floors.Count == 0)
        {
            throw new InvalidOperationException("World Tower must define at least one floor.");
        }

        var expected = 1;
        var previousTowerTokens = 0;
        foreach (var floor in floors)
        {
            if (floor.FloorNumber != expected++)
                throw new InvalidOperationException("World Tower floor numbers must be contiguous and start at 1.");
            if (string.IsNullOrWhiteSpace(floor.Name) || string.IsNullOrWhiteSpace(floor.GuardianName))
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} is missing a name.");
            if (floor.GuardianCreatureId == Guid.Empty)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has no Guardian creature.");
            if (string.IsNullOrWhiteSpace(floor.GuardianAbilityProfileId))
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has no Guardian ability profile.");
            if (floor.RequiredSlots <= 0 || floor.RequiredSlots > 100)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has invalid Expedition slots.");
            if (floor.RecommendedPowerRating < 0)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has an invalid recommended Power Rating.");
            if (floor.Type == TowerFloorType.Sovereign && floor.EchoEnabledAfterClear)
                throw new InvalidOperationException($"World Tower Sovereign floor {floor.FloorNumber} cannot enable Echo Mode.");
            if (floor.TowerTokens <= previousTowerTokens)
            {
                throw new InvalidOperationException(
                    $"World Tower floor {floor.FloorNumber} must award more Tower Tokens than the previous floor.");
            }
            previousTowerTokens = floor.TowerTokens;
            if (!IsValidScaling(floor.GuardianScaling))
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has invalid Guardian scaling.");
            if (floor.BalanceBenchmark.CharacterLevel <= 0
                || floor.BalanceBenchmark.EquipmentTier != 1
                || floor.BalanceBenchmark.EquipmentRarity > Domain.Models.Items.Rarity.Legendary
                || floor.BalanceBenchmark.EssenceCount is < 1 or > 6
                || floor.BalanceBenchmark.EssenceCount >
                    Math.Clamp(floor.BalanceBenchmark.CharacterLevel / 10 + 1, 1, 10))
            {
                throw new InvalidOperationException(
                    $"World Tower floor {floor.FloorNumber} has an invalid Tier 1 balance benchmark.");
            }
            if (floor.Unlocks.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Description))
                || floor.Unlocks.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != floor.Unlocks.Count)
            {
                throw new InvalidOperationException(
                    $"World Tower floor {floor.FloorNumber} has invalid or duplicate unlock definitions.");
            }
        }

        var echoUnlockFloors = floors
            .Where(floor => floor.Unlocks.Any(unlock =>
                string.Equals(
                    unlock.Key,
                    "tower_echo_mode_unlock",
                    StringComparison.OrdinalIgnoreCase)))
            .Select(floor => floor.FloorNumber)
            .ToArray();
        if (echoUnlockFloors.Length != 1)
        {
            throw new InvalidOperationException(
                "World Tower must define the Echo Mode unlock on exactly one floor.");
        }
    }

    private static void ValidateRewardCurve(TowerRewardCurveDefinition curve)
    {
        if (curve.BaseReward <= 0
            || curve.MaximumFloor is <= 0 or > 10_000
            || !double.IsFinite(curve.MaximumMultiplier)
            || curve.MaximumMultiplier <= 1
            || !double.IsFinite(curve.Exponent)
            || curve.Exponent <= 0)
        {
            throw new InvalidOperationException("World Tower has an invalid reward curve.");
        }

        try
        {
            var previousReward = 0;
            for (var floorNumber = 1; floorNumber <= curve.MaximumFloor; floorNumber++)
            {
                var reward = curve.Calculate(floorNumber);
                if (reward <= previousReward)
                {
                    throw new InvalidOperationException(
                        $"World Tower reward curve must increase on every floor; floor {floorNumber} awards {reward}.");
                }

                previousReward = reward;
            }
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException("World Tower reward curve exceeds the supported reward range.", exception);
        }
    }

    private static bool IsValidScaling(TowerGuardianScalingDefinition scaling) =>
        new[]
        {
            scaling.Health,
            scaling.Offense,
            scaling.Defense,
            scaling.Resistance,
            scaling.Penetration,
            scaling.Regeneration
        }.All(value => float.IsFinite(value) && value > 0);
}
