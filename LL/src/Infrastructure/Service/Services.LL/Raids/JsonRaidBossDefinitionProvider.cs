using System.Text.Json;
using Application.Interfaces.Services.LL.Raids;
using Domain.Models.Raids;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Raids;

public sealed class JsonRaidBossDefinitionProvider : IRaidBossDefinitionProvider
{
    private readonly IReadOnlyList<RaidBossDefinition> definitions;
    private readonly IReadOnlyDictionary<string, RaidBossDefinition> byId;

    public JsonRaidBossDefinitionProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "raids", "raid-bosses.json");
        var document = JsonSerializer.Deserialize<RaidBossCatalogDocument>(File.ReadAllText(path), jsonOptions)
            ?? throw new InvalidOperationException("Raid boss definitions could not be loaded.");

        definitions = document.RaidBosses;
        Validate(definitions);
        byId = definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RaidBossDefinition> GetAll() => definitions;

    public RaidBossDefinition? Get(string raidBossId) => byId.GetValueOrDefault(raidBossId);

    private static void Validate(IReadOnlyList<RaidBossDefinition> raidBosses)
    {
        if (raidBosses.Count == 0)
            throw new InvalidOperationException("At least one raid boss definition is required.");
        var duplicate = raidBosses.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate raid boss id '{duplicate.Key}'.");

        foreach (var boss in raidBosses)
        {
            if (string.IsNullOrWhiteSpace(boss.Id) || string.IsNullOrWhiteSpace(boss.Name))
                throw new InvalidOperationException("Raid boss id and name are required.");
            if (boss.Region <= 0 || boss.Regions.Any(x => x <= 0) || boss.LevelRequirement <= 0 || boss.Tiers.Count == 0)
                throw new InvalidOperationException($"Raid boss '{boss.Id}' has invalid region, level, or tiers.");
            if (boss.Tiers.Select(x => x.Tier).Distinct().Count() != boss.Tiers.Count)
                throw new InvalidOperationException($"Raid boss '{boss.Id}' has duplicate tiers.");

            foreach (var tier in boss.Tiers)
            {
                if (tier.Tier <= 0 || tier.LaneSlots is <= 0 or > 5 || tier.MinimumRoster <= 0 || tier.MinimumRoster > tier.LaneSlots * 3)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} has invalid roster settings.");
                if (tier.SignupWindowHours <= 0
                    || tier.TickBudget.Rearguard <= 0
                    || tier.TickBudget.Vanguard <= 0
                    || tier.TickBudget.MainGuard <= 0
                    || tier.TickBudget.FinalAssault <= 0)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} has invalid timing settings.");
                if (tier.Boss.CreatureId == Guid.Empty
                    || tier.Vanguard.GuardianCreatureId == Guid.Empty
                    || tier.MainGuard.ProjectionCreatureId == Guid.Empty
                    || tier.Rearguard.Adds.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Raid boss '{boss.Id}' tier {tier.Tier} must author a final boss, Rearguard adds, a Vanguard guardian, and a Main Guard projection.");
                }
                if (tier.Rearguard.WaveCount is <= 0 or > RaidRearguardDefinition.MaximumWaveCount)
                {
                    throw new InvalidOperationException(
                        $"Raid boss '{boss.Id}' tier {tier.Tier} must author between 1 and {RaidRearguardDefinition.MaximumWaveCount} Rearguard waves.");
                }
                if (tier.Rearguard.Adds.Concat(tier.Vanguard.Escorts).Any(x =>
                        x.CreatureId == Guid.Empty ||
                        x.Count <= 0 ||
                        x.SpawnChancePercent is <= 0 or > 100))
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} contains an invalid creature group.");
                if (tier.MainGuard.SurvivalThresholdsPercent.Count == 0
                    || tier.MainGuard.SurvivalThresholdsPercent.Any(x => x is <= 0 or > 100)
                    || !tier.MainGuard.SurvivalThresholdsPercent.SequenceEqual(
                        tier.MainGuard.SurvivalThresholdsPercent.Order()))
                {
                    throw new InvalidOperationException(
                        $"Raid boss '{boss.Id}' tier {tier.Tier} has invalid Main Guard survival thresholds.");
                }
                if (tier.Boss.Variants.Any(x => x.CreatureId == Guid.Empty || x.SpawnChancePercent is <= 0 or > 100) ||
                    tier.Boss.Variants.Sum(x => x.SpawnChancePercent) > 100)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} contains invalid boss variants.");
                if (tier.Boss.Stagger is not null && !IsValidStagger(tier.Boss.Stagger))
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} has invalid Stagger settings.");
            }
        }
    }

    private static bool IsValidStagger(Domain.Models.Combat.BossStaggerDefinition stagger) =>
        !stagger.Enabled
        || (stagger.BaseThreshold > 0
            && stagger.ReferenceParticipantCount > 0
            && double.IsFinite(stagger.ParticipantExponent)
            && stagger.ParticipantExponent is >= 0.5d and <= 1.5d
            && stagger.BreakDurationTicks is > 0 and <= 300
            && stagger.RecoveryDurationTicks is >= 0 and <= 600
            && stagger.DamageTakenBonusPercent is >= 0 and <= 100
            && stagger.ThresholdGrowthPercentPerBreak is >= 0 and <= 500
            && stagger.MaximumBreaks is null or > 0);
}
