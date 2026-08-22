using System.Text.Json;
using Application.Interfaces.Services.LL.RegionBosses;
using Domain.Models.RegionBosses;
using Microsoft.Extensions.Configuration;

namespace Services.LL.RegionBosses;

public sealed class JsonRegionBossDefinitionProvider : IRegionBossDefinitionProvider
{
    private readonly IReadOnlyList<RegionBossDefinition> definitions;
    private readonly IReadOnlyDictionary<string, RegionBossDefinition> byId;

    public JsonRegionBossDefinitionProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "region-bosses", "region-bosses.json");
        var document = JsonSerializer.Deserialize<RegionBossCatalogDocument>(File.ReadAllText(path), jsonOptions)
            ?? throw new InvalidOperationException("Region Boss definitions could not be loaded.");
        definitions = document.RegionBosses;
        Validate(definitions);
        byId = definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RegionBossDefinition> GetAll() => definitions;
    public RegionBossDefinition? Get(string definitionId) => byId.GetValueOrDefault(definitionId);

    private static void Validate(IReadOnlyList<RegionBossDefinition> definitions)
    {
        if (definitions.Count == 0)
            throw new InvalidOperationException("At least one Region Boss definition is required.");
        var duplicate = definitions.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate Region Boss id '{duplicate.Key}'.");
        foreach (var boss in definitions)
        {
            if (string.IsNullOrWhiteSpace(boss.Id) || string.IsNullOrWhiteSpace(boss.Name)
                || string.IsNullOrWhiteSpace(boss.ImagePath) || boss.RegionId <= 0
                || boss.CreatureId == Guid.Empty || boss.LevelRequirement <= 0
                || boss.RequiredTowerFloor is <= 0)
                throw new InvalidOperationException($"Region Boss '{boss.Id}' has invalid identity or access settings.");
            if (!IsPositive(boss.BaseScaling.Health) || !IsPositive(boss.BaseScaling.Power)
                || !IsPositive(boss.BaseScaling.Armor) || !IsPositive(boss.BaseScaling.Resistance)
                || !IsPositive(boss.BaseScaling.Penetration) || !IsPositive(boss.BaseScaling.Regeneration)
                || !IsAtLeastOne(boss.LevelScaling.HealthGrowth) || !IsAtLeastOne(boss.LevelScaling.PowerGrowth)
                || !IsNonNegative(boss.LevelScaling.ArmorGrowthPerLevel)
                || !IsNonNegative(boss.LevelScaling.ResistanceGrowthPerLevel)
                || !IsNonNegative(boss.LevelScaling.PenetrationGrowthPerLevel)
                || boss.Fury.IntervalSeconds <= 0 || !IsNonNegative(boss.Fury.PowerPercentPerStack)
                || !IsNonNegative(boss.Fury.AttackSpeedPercentPerStack))
                throw new InvalidOperationException($"Region Boss '{boss.Id}' has invalid combat scaling.");
            if (boss.Revival.BaseDelaySeconds <= 0 || boss.Revival.AdditionalDelaySecondsPerDeath < 0
                || boss.Revival.MaximumDelaySeconds < boss.Revival.BaseDelaySeconds
                || !IsPercentage(boss.Revival.ReviveHealthPercent, allowZero: false)
                || !IsPercentage(boss.Recovery.LivingHealPercent, allowZero: true)
                || !IsPercentage(boss.Recovery.DownedReviveHealthPercent, allowZero: false))
                throw new InvalidOperationException($"Region Boss '{boss.Id}' has invalid recovery settings.");
            if (boss.Schedule.MinimumIntervalHours <= 0
                || boss.Schedule.MaximumIntervalHours < boss.Schedule.MinimumIntervalHours
                || boss.Schedule.SignupDurationMinutes <= 0
                || TimeSpan.FromMinutes(boss.Schedule.SignupDurationMinutes)
                    >= TimeSpan.FromHours(boss.Schedule.MinimumIntervalHours))
                throw new InvalidOperationException($"Region Boss '{boss.Id}' has invalid schedule settings.");
            if (boss.RewardBrackets.Count == 0
                || boss.RewardBrackets.Any(x => string.IsNullOrWhiteSpace(x.Key)
                    || x.MinimumLevelDefeated <= 0 || x.Cinders < 0 || x.Soulstones < 0
                    || x.Cinders == 0 && x.Soulstones == 0)
                || boss.RewardBrackets.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)
                || boss.RewardBrackets.GroupBy(x => x.MinimumLevelDefeated).Any(x => x.Count() > 1))
                throw new InvalidOperationException($"Region Boss '{boss.Id}' has invalid reward brackets.");
        }
    }

    private static bool IsPositive(double value) => double.IsFinite(value) && value > 0;
    private static bool IsAtLeastOne(double value) => double.IsFinite(value) && value >= 1;
    private static bool IsNonNegative(double value) => double.IsFinite(value) && value >= 0;
    private static bool IsPercentage(double value, bool allowZero) =>
        double.IsFinite(value) && value <= 100 && (allowZero ? value >= 0 : value > 0);
}
