using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;

namespace Domain.Extensions.Soulstones;
public static class UpgradeStatExtensions
{
    public static double GetStatBonus(this IEnumerable<CharacterSoulstoneUpgrade> upgrades, IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> defs, string statName)
    {
        return upgrades.Sum(u =>
        {
            if (!defs.TryGetValue(u.SoulstoneUpgradeDefinitionId, out var def))
                return 0;

            return def.Effect.Stat.Equals(statName, StringComparison.OrdinalIgnoreCase)
                 ? def.Effect.PerLevel * u.Level
                 : 0;
        });
    }
}
