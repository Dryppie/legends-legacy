using Domain.Models.Combat.Abilities;

namespace Application.UseCases.Essences.Dtos;

internal static class AbilityTargetMapping
{
    public static IReadOnlyList<string> GetDistinctTargets(AbilitySpec ability) =>
        ability.Effects
            .Select(effect => effect.Target.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
