using Domain.Models.Bonuses;
using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;
using Services.LL.Interfaces;
using Services.LL.Providers;

namespace Services.LL.Bonuses;
public sealed class SoulstoneBonusProvider : IBonusProvider
{
    private readonly ISoulstoneUpgradeRepository _repo;
    private readonly IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> _defs;

    public SoulstoneBonusProvider(
        ISoulstoneUpgradeRepository repo,
        SoulstoneUpgradeDefinitionProvider defProvider)
    {
        _repo = repo;
        _defs = defProvider.All;          // hot-reloaded view
    }

    public async ValueTask<IReadOnlyCollection<Bonus>> GetBonusesAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
    {
        // 2. fetch only the character’s owned upgrades (cheap query)              // no filter – we want *all*
        var owned = await _repo.GetSoulstoneUpgradesByCharacterIdAsync(characterId,[], ct);

        var list = new List<Bonus>(owned.Count);

        foreach (var up in owned)
        {
            if (!_defs.TryGetValue(up.SoulstoneUpgradeDefinitionId, out var def))
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
