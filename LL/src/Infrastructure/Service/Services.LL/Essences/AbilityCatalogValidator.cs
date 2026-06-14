using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;

namespace Services.LL.Essences;

public sealed class AbilityCatalogValidator : IAbilityCatalogValidator
{
    private static readonly IReadOnlySet<string> SupportedEffects = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AbilityEffectType.Damage,
        AbilityEffectType.Heal,
        AbilityEffectType.ApplyStatus,
        AbilityEffectType.ModifyStatusEffect,
        AbilityEffectType.RemoveStatus,
        AbilityEffectType.Cleanse,
        AbilityEffectType.GrantBarrier,
        AbilityEffectType.ModifyAttribute,
        AbilityEffectType.RestoreResource,
        AbilityEffectType.Summon,
        AbilityEffectType.Taunt,
        AbilityEffectType.ReflectDamage,
        AbilityEffectType.AbsorbDamage,
        AbilityEffectType.TriggerSecondaryEffect
    };

    private static readonly IReadOnlySet<string> SupportedTriggers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AbilityTriggerType.OnCombatStart,
        AbilityTriggerType.OnAbilityUse,
        AbilityTriggerType.OnAbilityUsed,
        AbilityTriggerType.OnBasicAttack,
        AbilityTriggerType.OnHit,
        AbilityTriggerType.OnMeleeAttack,
        AbilityTriggerType.OnRangedAttack,
        AbilityTriggerType.OnAttacked,
        AbilityTriggerType.OnDamaged,
        AbilityTriggerType.OnMeleeAttacked,
        AbilityTriggerType.OnRangedAttacked,
        AbilityTriggerType.OnHealthChanged,
        AbilityTriggerType.OnCrit,
        AbilityTriggerType.OnTakeDamage,
        AbilityTriggerType.OnKill,
        AbilityTriggerType.OnDodge,
        AbilityTriggerType.OnStatusApplied,
        AbilityTriggerType.OnStatusExpired,
        AbilityTriggerType.OnInterval,
        AbilityTriggerType.OnDeath,
        AbilityTriggerType.OnHeal,
        AbilityTriggerType.OnHealed,
        AbilityTriggerType.OnLifestealHeal
    };

    private static readonly IReadOnlySet<string> SupportedConditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AbilityConditionType.Always,
        AbilityConditionType.TargetHealthBelowPercent,
        AbilityConditionType.SourceHealthBelowPercent,
        AbilityConditionType.SourceHealthAbovePercent,
        AbilityConditionType.TargetHasStatus,
        AbilityConditionType.TargetHasStatusStacksAtLeast,
        AbilityConditionType.SourceHasStatus,
        AbilityConditionType.RandomChance,
        AbilityConditionType.ChanceRoll,
        AbilityConditionType.CooldownReady,
        AbilityConditionType.SourceHasTag,
        AbilityConditionType.TargetHasTag,
        AbilityConditionType.IsSpecies,
        AbilityConditionType.SourceIsSummon
    };

    public IReadOnlyList<string> Validate(IReadOnlyList<AbilityDefinition> abilities)
    {
        var errors = new List<string>();
        var duplicateIds = abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);

        errors.AddRange(duplicateIds.Select(id => $"Duplicate Ability definition id '{id}'."));

        foreach (var ability in abilities)
        {
            ValidateAbility(ability, errors);
        }

        return errors;
    }

    public void ThrowIfInvalid(IReadOnlyList<AbilityDefinition> abilities)
    {
        var errors = Validate(abilities);
        if (errors.Count > 0)
            throw new InvalidOperationException("Ability catalog validation failed: " + string.Join(" | ", errors));
    }

    public AbilityCatalogSupportMatrix GetSupportMatrix() =>
        new(
            Sort(AbilityEffectType.All),
            Sort(SupportedEffects),
            Sort(AbilityTriggerType.All),
            Sort(SupportedTriggers),
            Sort(AbilityConditionType.All),
            Sort(SupportedConditions),
            Sort(AbilityTargetSelector.All),
            Sort(AbilityTargetSelector.All));

    private static void ValidateAbility(AbilityDefinition ability, List<string> errors)
    {
        var label = string.IsNullOrWhiteSpace(ability.Id) ? "<missing id>" : ability.Id;

        if (string.IsNullOrWhiteSpace(ability.Id))
            errors.Add("Ability id is required.");

        if (string.IsNullOrWhiteSpace(ability.Kind) || !AbilityDefinitionKind.All.Contains(ability.Kind))
            errors.Add($"{label}: unknown ability kind '{ability.Kind}'.");

        if (string.IsNullOrWhiteSpace(ability.Name))
            errors.Add($"{label}: name is required.");

        if (!string.IsNullOrWhiteSpace(ability.Targeting) && !AbilityTargetSelector.All.Contains(ability.Targeting))
            errors.Add($"{label}: unknown target selector '{ability.Targeting}'.");

        foreach (var trigger in ability.Triggers)
        {
            if (string.IsNullOrWhiteSpace(trigger.Type))
            {
                errors.Add($"{label}: trigger type is required.");
                continue;
            }

            var normalized = AbilityTriggerType.Normalize(trigger.Type);
            if (!AbilityTriggerType.All.Contains(normalized))
                errors.Add($"{label}: unknown trigger '{trigger.Type}'.");
            else if (!SupportedTriggers.Contains(normalized))
                errors.Add($"{label}: trigger '{trigger.Type}' is valid content but is not supported by combat mapping.");
        }

        ValidateConditions(label, ability.Conditions, errors);

        var duplicateEffectIds = ability.Effects
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);

        errors.AddRange(duplicateEffectIds.Select(id => $"{label}: duplicate effect id '{id}'."));

        foreach (var effect in ability.Effects)
        {
            ValidateEffect(label, effect, errors);
        }
    }

    private static void ValidateEffect(string abilityId, AbilityEffectDefinition effect, List<string> errors)
    {
        var label = string.IsNullOrWhiteSpace(effect.Id) ? $"{abilityId}/<missing effect id>" : $"{abilityId}/{effect.Id}";

        if (string.IsNullOrWhiteSpace(effect.Id))
            errors.Add($"{abilityId}: effect id is required.");

        if (!AbilityEffectType.All.Contains(effect.Type))
            errors.Add($"{label}: unknown effect type '{effect.Type}'.");
        else if (!SupportedEffects.Contains(effect.Type))
            errors.Add($"{label}: effect type '{effect.Type}' is valid content but is not supported by combat mapping.");

        if (!AbilityTargetSelector.All.Contains(effect.Target))
            errors.Add($"{label}: unknown target selector '{effect.Target}'.");

        if (!string.IsNullOrWhiteSpace(effect.Attribute)
            && !Enum.TryParse<AttributeType>(effect.Attribute, ignoreCase: true, out _))
        {
            errors.Add($"{label}: unknown attribute '{effect.Attribute}'.");
        }

        if (effect.Type.Equals(AbilityEffectType.ModifyAttribute, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(effect.Attribute))
        {
            errors.Add($"{label}: ModifyAttribute requires attribute.");
        }

        if (effect.Type.Equals(AbilityEffectType.ApplyStatus, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(effect.Status))
        {
            errors.Add($"{label}: ApplyStatus requires status.");
        }

        ValidateConditions(label, effect.Conditions, errors);
    }

    private static void ValidateConditions(
        string ownerId,
        IEnumerable<AbilityConditionDefinition> conditions,
        List<string> errors)
    {
        foreach (var condition in conditions)
        {
            if (!AbilityConditionType.All.Contains(condition.Type))
            {
                errors.Add($"{ownerId}: unknown condition '{condition.Type}'.");
                continue;
            }

            if (!SupportedConditions.Contains(condition.Type))
                errors.Add($"{ownerId}: condition '{condition.Type}' is valid content but is not supported by combat mapping.");

            if ((condition.Type.Equals(AbilityConditionType.RandomChance, StringComparison.OrdinalIgnoreCase)
                 || condition.Type.Equals(AbilityConditionType.ChanceRoll, StringComparison.OrdinalIgnoreCase))
                && condition.Value is null or < 0 or > 100)
            {
                errors.Add($"{ownerId}: condition '{condition.Type}' requires a value from 0 to 100.");
            }
        }
    }

    private static IReadOnlyList<string> Sort(IEnumerable<string> values) =>
        values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
}
