using Application.Interfaces.Services.LL.Regions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Regions;

public sealed class JsonAreaExperienceBalanceProvider : IAreaExperienceBalanceProvider
{
    private const int DefaultEncounterCadenceSeconds = 10;
    private readonly IReadOnlyDictionary<string, AreaExperienceRate> _rates;

    public JsonAreaExperienceBalanceProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var worldPath = Path.Combine(contentRootPath, contentRoot, "world");
        var progressionPath = Path.Combine(contentRootPath, contentRoot, "progression");
        var regionDocument = Read<RegionDocument>(Path.Combine(worldPath, "regions.json"), options);
        var creatureDocument = Read<CreatureDocument>(Path.Combine(worldPath, "creatures.json"), options);
        var experienceDocument = Read<AreaExperienceDocument>(
            Path.Combine(progressionPath, "area-experience.json"),
            options);
        var encounterCadenceSeconds = config.GetValue(
            "Combat:IdleProgression:EncounterCadenceSeconds",
            DefaultEncounterCadenceSeconds);

        ValidateSettings(experienceDocument.AreaExperience, encounterCadenceSeconds);

        var areas = regionDocument.Regions.SelectMany(x => x.Areas).ToList();
        var duplicateAreaIds = areas
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateAreaIds.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate area ids: {string.Join(", ", duplicateAreaIds)}.");
        }

        var creatureIds = creatureDocument.Creatures.Select(x => x.Id).ToHashSet();
        _rates = areas.ToDictionary(
            x => x.Id,
            area => CreateRate(
                area,
                creatureIds,
                experienceDocument.AreaExperience,
                encounterCadenceSeconds),
            StringComparer.OrdinalIgnoreCase);
    }

    public decimal GetTargetExperiencePerHour(string areaId) => GetRate(areaId).TargetExperiencePerHour;
    public decimal GetTargetCindersPerHour(string areaId) => GetRate(areaId).TargetCindersPerHour;

    public int CalculateEncounterExperience(string areaId, int creatureCount)
        => CalculateEncounterValue(areaId, creatureCount, GetRate(areaId).ExperiencePerCreature, "XP");

    public int CalculateEncounterCinders(string areaId, int creatureCount)
        => CalculateEncounterValue(areaId, creatureCount, GetRate(areaId).CindersPerCreature, "Cinders");

    private static int CalculateEncounterValue(
        string areaId,
        int creatureCount,
        decimal valuePerCreature,
        string rewardName)
    {
        if (creatureCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creatureCount), "An encounter must contain a creature.");
        }

        var value = valuePerCreature * creatureCount;
        if (value > int.MaxValue)
        {
            throw new OverflowException($"Encounter {rewardName} for area '{areaId}' exceeds the supported range.");
        }

        return decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));
    }

    private static T Read<T>(string path, JsonSerializerOptions options) where T : new() =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), options) ?? new T();

    private AreaExperienceRate GetRate(string areaId)
    {
        if (string.IsNullOrWhiteSpace(areaId) || !_rates.TryGetValue(areaId, out var rate))
        {
            throw new KeyNotFoundException($"No area XP balance exists for area '{areaId}'.");
        }

        return rate;
    }

    private static AreaExperienceRate CreateRate(
        AreaDefinition area,
        IReadOnlySet<Guid> creatureIds,
        AreaExperienceSettings settings,
        int encounterCadenceSeconds)
    {
        if (string.IsNullOrWhiteSpace(area.Id) ||
            area.DifficultyTier < 0 ||
            area.SpawnProbabilities.Count == 0 ||
            area.Creatures.Count == 0 ||
            area.SpawnProbabilities.Any(x => x < 0) ||
            area.Creatures.Any(x => x.WeightedSpawnRate <= 0))
        {
            throw new InvalidOperationException($"Area '{area.Id}' has invalid spawn data.");
        }

        var probabilityTotal = area.SpawnProbabilities.Sum();
        var weightTotal = area.Creatures.Sum(x => x.WeightedSpawnRate);
        if (probabilityTotal <= 0 || weightTotal <= 0)
        {
            throw new InvalidOperationException($"Area '{area.Id}' has empty spawn weights.");
        }

        foreach (var spawn in area.Creatures)
        {
            if (!creatureIds.Contains(spawn.CreatureId))
            {
                throw new InvalidOperationException(
                    $"Area '{area.Id}' references missing creature '{spawn.CreatureId}'.");
            }
        }

        var expectedCreatureCount = area.SpawnProbabilities
            .Select((probability, index) => probability * (index + 1))
            .Sum() / probabilityTotal;
        var targetExperiencePerHour = settings.BaseExperiencePerHour;
        var targetCindersPerHour = settings.BaseCindersPerHour;
        for (var tier = 0; tier < area.DifficultyTier; tier++)
        {
            targetExperiencePerHour = checked(
                targetExperiencePerHour * settings.DifficultyTierMultiplier);
            targetCindersPerHour = checked(
                targetCindersPerHour * settings.DifficultyTierMultiplier);
        }

        var encountersPerHour = 3_600m / encounterCadenceSeconds;
        var experiencePerCreature = targetExperiencePerHour /
                                    encountersPerHour /
                                    expectedCreatureCount;
        var cindersPerCreature = targetCindersPerHour /
                                 encountersPerHour /
                                 expectedCreatureCount;

        return new AreaExperienceRate(
            targetExperiencePerHour,
            targetCindersPerHour,
            experiencePerCreature,
            cindersPerCreature);
    }

    private static void ValidateSettings(AreaExperienceSettings settings, int encounterCadenceSeconds)
    {
        if (settings.BaseExperiencePerHour <= 0 ||
            settings.BaseCindersPerHour <= 0 ||
            settings.DifficultyTierMultiplier < 1 ||
            encounterCadenceSeconds <= 0)
        {
            throw new InvalidOperationException("Area XP progression settings are invalid.");
        }
    }

    private sealed class RegionDocument
    {
        public List<RegionDefinition> Regions { get; set; } = [];
    }

    private sealed class RegionDefinition
    {
        public List<AreaDefinition> Areas { get; set; } = [];
    }

    private sealed class AreaDefinition
    {
        public string Id { get; set; } = string.Empty;
        public int DifficultyTier { get; set; }
        public List<decimal> SpawnProbabilities { get; set; } = [];
        public List<AreaCreatureDefinition> Creatures { get; set; } = [];
    }

    private sealed class AreaCreatureDefinition
    {
        public Guid CreatureId { get; set; }
        public decimal WeightedSpawnRate { get; set; }
    }

    private sealed class CreatureDocument
    {
        public List<CreatureDefinition> Creatures { get; set; } = [];
    }

    private sealed class CreatureDefinition
    {
        public Guid Id { get; set; }
    }

    private sealed class AreaExperienceDocument
    {
        public AreaExperienceSettings AreaExperience { get; set; } = new();
    }

    private sealed class AreaExperienceSettings
    {
        public decimal BaseExperiencePerHour { get; set; }
        public decimal BaseCindersPerHour { get; set; }
        public decimal DifficultyTierMultiplier { get; set; }
    }

    private sealed record AreaExperienceRate(
        decimal TargetExperiencePerHour,
        decimal TargetCindersPerHour,
        decimal ExperiencePerCreature,
        decimal CindersPerCreature);
}
