using Domain.Models.Bonuses;
using Domain.Models.Soulstones;
using Services.LL.Interfaces;
using Services.LL.Providers;

namespace Services.LL.Bonuses;

public sealed class SoulstoneBonusProvider : IBonusProvider
{
    private readonly ISoulstoneUpgradeRepository _repo;
    private readonly SoulstoneUpgradeDefinitionProvider _defProvider;

    public SoulstoneBonusProvider(ISoulstoneUpgradeRepository repo, SoulstoneUpgradeDefinitionProvider defProvider)
    {
        _repo = repo;
        _defProvider = defProvider;
    }

    public async ValueTask<IReadOnlyCollection<Bonus>> GetBonusesAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
    {
        var owned = await _repo.GetSoulstoneUpgradesByCharacterIdAsync(characterId, [], ct);
        var bonuses = new List<Bonus>();

        foreach (var upgrade in owned.Where(x => x.Level > 0))
        {
            if (!_defProvider.All.TryGetValue(upgrade.SoulstoneUpgradeDefinitionId, out var def) || !def.Enabled)
            {
                continue;
            }

            var rank = Math.Clamp(upgrade.Level, 1, def.MaxRank);
            foreach (var effect in def.Effects)
            {
                if (!Enum.TryParse<BonusKind>(effect.Kind.ToString(), out var kind))
                {
                    continue;
                }

                bonuses.Add(new Bonus(kind, effect.ValuesByRank[rank - 1]));
            }
        }

        return bonuses;
    }
}
