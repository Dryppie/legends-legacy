using Application.Interfaces.Services.LL;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Services.LL.Providers;

namespace Services.LL.Guilds;
public class GuildBuildingUpgradeService : IGuildBuildingUpgradeService
{
    private readonly IGuildService _guildService;
    private readonly IReadOnlyDictionary<string, BuildingUpgradeDefinition> _defs;

    public GuildBuildingUpgradeService(IGuildService guildService, GuildBuildingUpgradeDefinitionProvider provider)
    {
        _guildService = guildService;
        _defs = provider.All;
    }

    public async Task<List<BuildingUpgradeView>> GetForGuildAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildWithUpgradesAsync(characterId, cancellationToken);
        if (guild == null) return [];

        var levels = guild.GuildBuildingUpgrades.ToDictionary(u => u.BuildingUpgradeDefinitionId, u => u.Level);

        return _defs.Values.Select(def =>
        {
            levels.TryGetValue(def.Id, out var lvl);

            var nextCost = lvl < def.MaxLevel ? GetCostForLevel(def, lvl + 1) : null;
            return new BuildingUpgradeView(def, lvl, nextCost);
        }).ToList();
    }

    public async Task<bool> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildWithUpgradesAsync(characterId, cancellationToken);
        if (guild == null) return false;
        var guildMember = guild.Members.FirstOrDefault(gm => gm.CharacterId == characterId);
        if (guildMember == null || !guildMember.IsGuildLeader()) return false;

        if (!_defs.TryGetValue(upgradeId, out var def)) return false;

        var upgrade = guild.GuildBuildingUpgrades
            .FirstOrDefault(u => u.BuildingUpgradeDefinitionId == upgradeId);

        var level = upgrade?.Level ?? 0;
        if (level >= def.MaxLevel) return false;

        var cost = GetCostForLevel(def, level + 1);
        if (!TrySpendResources(guild, cost)) return false;

        if (upgrade is null)
        {
            guild.GuildBuildingUpgrades.Add(new GuildBuildingUpgrade
            {
                GuildId = guild.Id,
                BuildingUpgradeDefinitionId = upgradeId,
                Level = 1
            });
        }
        else
        {
            upgrade.Level++;
        }

        await _guildService.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Dictionary<GuildResourceType, int> GetCostForLevel(BuildingUpgradeDefinition def, int level)
    {
        return def.CostCurves.ToDictionary(
            c => c.Resource,
            c => CalculateCost(level, c.Base, c.Increment, c.IncrementCap)
        );
    }

    private static int CalculateCost(int level, int baseValue, int increment, int? incrementCap)
    {
        int effectiveIncrement = incrementCap.HasValue
            ? Math.Min(level - 1, incrementCap.Value)
            : level - 1;

        return baseValue + (increment * effectiveIncrement);
    }

    private static bool TrySpendResources(Guild guild, Dictionary<GuildResourceType, int> cost)
    {
        // Check all costs
        foreach (var kvp in cost)
        {
            var res = guild.Resources.FirstOrDefault(r => r.Resource.Equals(kvp.Key));
            if (res == null || res.Amount < kvp.Value)
                return false;
        }

        // All passed – deduct
        foreach (var kvp in cost)
        {
            var res = guild.Resources.FirstOrDefault(r => r.Resource.Equals(kvp.Key));
            if (res != null)
            {
                res.Amount -= kvp.Value;
            }
        }

        return true;
    }
}
