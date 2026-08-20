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
                if (tier.SignupWindowHours <= 0 || tier.TickBudget.Vanguard <= 0 || tier.TickBudget.Flank <= 0 || tier.TickBudget.Ward <= 0)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} has invalid timing settings.");
                if (string.IsNullOrWhiteSpace(tier.RaidSealItemId) ||
                    string.IsNullOrWhiteSpace(tier.RaidSealFragmentItemId) ||
                    tier.RaidSealFragmentCost != RaidRules.RaidSealFragmentCost)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} has invalid Raid Seal settings.");
                if (tier.Boss.CreatureId == Guid.Empty || tier.Ward.ObjectiveCreatureId == Guid.Empty || tier.Flank.Adds.Count == 0)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} must author a boss, Flank adds, and a Ward objective.");
                if (tier.Flank.Adds.Concat(tier.Ward.Guards).Any(x =>
                        x.CreatureId == Guid.Empty ||
                        x.Count <= 0 ||
                        x.SpawnChancePercent is <= 0 or > 100))
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} contains an invalid creature group.");
                if (tier.Boss.Variants.Any(x => x.CreatureId == Guid.Empty || x.SpawnChancePercent is <= 0 or > 100) ||
                    tier.Boss.Variants.Sum(x => x.SpawnChancePercent) > 100)
                    throw new InvalidOperationException($"Raid boss '{boss.Id}' tier {tier.Tier} contains invalid boss variants.");
            }
        }
    }
}
