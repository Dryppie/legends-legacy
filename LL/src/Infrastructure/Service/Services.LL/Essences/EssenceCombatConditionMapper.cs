using Domain.Interfaces.Combat.Abilities;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;

namespace Services.LL.Essences;

internal static class EssenceCombatConditionMapper
{
    public static ICondition Build(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var mapped = conditions
            .Select(Map)
            .Where(x => x is not null)
            .Cast<ICondition>()
            .ToList();

        return mapped.Count switch
        {
            0 => new NoCondition(),
            1 => mapped[0],
            _ => new AllConditions(mapped)
        };
    }

    public static int BuildChance(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var chance = conditions.FirstOrDefault(x =>
            x.Type.Equals(AbilityConditionType.RandomChance, StringComparison.OrdinalIgnoreCase)
            || x.Type.Equals(AbilityConditionType.ChanceRoll, StringComparison.OrdinalIgnoreCase));
        return chance?.Value is > 0 ? Math.Clamp((int)Math.Round(chance.Value.Value), 1, 100) : 100;
    }

    private static ICondition? Map(AbilityConditionDefinition condition) =>
        condition.Type switch
        {
            AbilityConditionType.TargetHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            "HealthBelowPercent" when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.SourceHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: true, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.SourceHealthAbovePercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: true, (int)Math.Round(condition.Value.Value), ComparisonType.GreaterThan),
            AbilityConditionType.TargetHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: false, condition.Status),
            AbilityConditionType.TargetHasStatusStacksAtLeast when !string.IsNullOrWhiteSpace(condition.Status)
                && condition.Value is > 0
                && Enum.TryParse<StatusEffectType>(condition.Status, ignoreCase: true, out var statusEffect) =>
                new CombatantStatusStacksCondition(useSource: false, statusEffect, (int)Math.Round(condition.Value.Value)),
            AbilityConditionType.SourceHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: true, condition.Status),
            AbilityConditionType.RandomChance => null,
            AbilityConditionType.ChanceRoll => null,
            AbilityConditionType.CooldownReady => null,
            AbilityConditionType.Always => null,
            AbilityConditionType.SourceHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: true, condition.Tag),
            AbilityConditionType.TargetHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, condition.Tag),
            AbilityConditionType.IsSpecies when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, NormalizeSpeciesTag(condition.Tag)),
            AbilityConditionType.SourceIsSummon =>
                new CombatantSummonedCondition(useSource: true),
            _ => null
        };

    private static string NormalizeSpeciesTag(string tag) =>
        tag.StartsWith("Species.", StringComparison.OrdinalIgnoreCase) ? tag : $"Species.{tag}";
}
