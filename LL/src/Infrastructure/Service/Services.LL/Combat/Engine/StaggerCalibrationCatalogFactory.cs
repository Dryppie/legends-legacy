using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Raids;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Combat.Engine;

/// <summary>
/// Builds an offline Stagger calibration catalog from the same authored Tower and Raid
/// definitions used by the live services. The catalog never changes runtime content.
/// </summary>
public sealed class StaggerCalibrationCatalogFactory
{
    private static readonly int[] RaidPlusLevels = [3, 6];

    private readonly WorldTowerCatalogDocument _tower;
    private readonly RaidBossCatalogDocument _raids;

    public StaggerCalibrationCatalogFactory(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var dataRoot = Path.Combine(
            contentRootPath,
            configuration["Content:Root"] ?? "Data");
        _tower = Read<WorldTowerCatalogDocument>(
            Path.Combine(dataRoot, "world-tower", "tower-floors.json"),
            jsonOptions);
        _raids = Read<RaidBossCatalogDocument>(
            Path.Combine(dataRoot, "raids", "raid-bosses.json"),
            jsonOptions);
    }

    public StaggerCalibrationCatalog CreateCatalog()
    {
        var encounters = new List<StaggerCalibrationEncounter>();
        encounters.AddRange(_tower.Floors
            .Where(floor => floor.Stagger is { Enabled: true })
            .Select(floor => new StaggerCalibrationEncounter(
                $"tower.floor-{floor.FloorNumber:00}",
                StaggerCalibrationContentType.Tower,
                $"Tower Floor {floor.FloorNumber}: {floor.Name}",
                floor.Stagger!,
                $"tower-floors.json floor {floor.FloorNumber}")));

        foreach (var raid in _raids.RaidBosses)
        {
            encounters.AddRange(raid.Tiers
                .Where(tier => tier.Boss.Stagger is { Enabled: true })
                .Select(tier => new StaggerCalibrationEncounter(
                    $"{raid.Id}.tier-{tier.Tier}",
                    StaggerCalibrationContentType.Raid,
                    $"{raid.Name} Tier {tier.Tier}",
                    tier.Boss.Stagger!,
                    $"raid-bosses.json {raid.Id} tier {tier.Tier}")));

            if (!raid.Tiers.Any(tier => tier.Boss.Stagger is { Enabled: true }))
                continue;

            foreach (var plusLevel in RaidPlusLevels)
            {
                var plus = RaidPlusDifficulty.Create(raid, plusLevel);
                if (plus.Boss.Stagger is not { Enabled: true } stagger)
                    continue;

                encounters.Add(new StaggerCalibrationEncounter(
                    $"{raid.Id}.plus-{plusLevel}",
                    StaggerCalibrationContentType.RaidPlus,
                    $"{raid.Name} +{plusLevel}",
                    stagger,
                    $"RaidPlusDifficulty.Create({raid.Id}, {plusLevel})"));
            }
        }

        if (encounters.Count == 0)
            throw new InvalidOperationException("No Stagger-enabled Tower or Raid encounters were found.");

        return new StaggerCalibrationCatalog(
            Version: 1,
            EvaluationDurationTicks: 1_800,
            Seeds:
            [
                17, 29, 43, 61, 79, 101, 127, 149,
                173, 197, 223, 251, 277, 307, 337, 367
            ],
            Cohorts:
            [
                new StaggerCalibrationParticipantCohort("undersized", 0.67d, false),
                new StaggerCalibrationParticipantCohort("reference", 1d, true),
                new StaggerCalibrationParticipantCohort("oversized", 1.33d, false)
            ],
            Profiles:
            [
                new StaggerCalibrationControlProfile(
                    "control-light",
                    "One occasional low-power control contributor per ten participants.",
                    0.10d,
                    25,
                    300,
                    80,
                    0,
                    1,
                    900,
                    null,
                    10d,
                    0.25d),
                new StaggerCalibrationControlProfile(
                    "balanced",
                    "One representative control contributor per five participants.",
                    0.20d,
                    35,
                    200,
                    80,
                    2,
                    3,
                    300,
                    900,
                    10d,
                    0.25d),
                new StaggerCalibrationControlProfile(
                    "control-heavy",
                    "Two coordinated control contributors per five participants.",
                    0.40d,
                    45,
                    150,
                    85,
                    3,
                    4,
                    150,
                    600,
                    10d,
                    1d)
            ],
            Encounters: encounters
                .OrderBy(encounter => encounter.ContentType)
                .ThenBy(encounter => encounter.Id, StringComparer.Ordinal)
                .ToList());
    }

    private static T Read<T>(string path, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
        ?? throw new InvalidOperationException($"Could not deserialize Stagger calibration source '{path}'.");
}
