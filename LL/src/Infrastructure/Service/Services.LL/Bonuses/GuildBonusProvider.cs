using Domain.Models.Bonuses;
using Domain.Models.Guilds.Buildings;
using Services.LL.Interfaces;
using Services.LL.Providers;

namespace Services.LL.Bonuses;
public class GuildBonusProvider : IBonusProvider
{
    private readonly IGuildBuildingUpgradeRepository _repo;
    private readonly IReadOnlyDictionary<string, BuildingUpgradeDefinition> _defs;

    public GuildBonusProvider(IGuildBuildingUpgradeRepository repo, GuildBuildingUpgradeDefinitionProvider defProvider)
    {
        _repo = repo;
        _defs = defProvider.All;
    }

    public async ValueTask<IReadOnlyCollection<Bonus>> GetBonusesAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
    {
        var owned = await _repo.GetGuildBuildingUpgradesByCharacterIdAsync(characterId, [], ct);

        var list = new List<Bonus>(owned.Count);

        foreach (var up in owned)
        {
            if (!_defs.TryGetValue(up.BuildingUpgradeDefinitionId, out var def))
                continue;                                   // invalid id – ignore

            // 3. flatten “per-level” into one number
            var value = def.Effect.PerLevel * up.Level;

            // 4. map the JSON Stat string -> enum (case-insensitive)
            if (!Enum.TryParse<BonusKind>(def.Effect.Stat, true, out var kind))
                continue;                                   // not used by combat yet

            list.Add(new Bonus(kind, value));
        }

        return list;
    }
}
