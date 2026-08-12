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

        _floors = document.Floors.OrderBy(x => x.FloorNumber).ToArray();
        Validate(_floors);
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
        foreach (var floor in floors)
        {
            if (floor.FloorNumber != expected++)
                throw new InvalidOperationException("World Tower floor numbers must be contiguous and start at 1.");
            if (string.IsNullOrWhiteSpace(floor.Name) || string.IsNullOrWhiteSpace(floor.GuardianName))
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} is missing a name.");
            if (floor.GuardianCreatureId == Guid.Empty)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has no Guardian creature.");
            if (floor.RequiredSlots <= 0 || floor.RequiredSlots > 100)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has invalid rally slots.");
            if (floor.RecommendedPowerRating < 0)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has an invalid recommended Power Rating.");
            if (!float.IsFinite(floor.GuardianStrengthMultiplier)
                || floor.GuardianStrengthMultiplier <= 1)
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has invalid Guardian scaling.");
            if (floor.ScoutingReveals.Any(x => x.Threshold is < 1 or > 100))
                throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has invalid scouting thresholds.");
        }
    }
}
