using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.Statuses;
using Domain.Models.Damages;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

internal static class CombatEffectActionHelpers
{
    public static SimpleCombatEntity CreateSimpleCombatEntity(CombatEntity target) =>
        new()
        {
            Id = target.Id,
            Name = target.Name,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetCurrentHealthValue(),
            Barrier = target.GetCurrentBarrierValue(),
            ImagePath = target.ImagePath
        };

    public static AbilityAttributeModifier CreateAttributeModifier(CombatEffectAction action) =>
        action.Attribute is null
            ? throw new InvalidOperationException("Attribute operation requires an attribute.")
            : new AbilityAttributeModifier(action.Attribute.Value, action.Magnitude, action.ModifierType);

    public static void ModifyStatusEffect(CombatEffectAction action, EffectContext effect, int amount)
    {
        if (string.IsNullOrWhiteSpace(action.StatusId))
            throw new InvalidOperationException("ModifyStatusEffect requires a status effect id.");

        var status = Enum.Parse<StatusEffectType>(action.StatusId, ignoreCase: true);
        effect.Target.ModifyStatusEffects(status, amount);
    }

    public static string FormatDamageAmount(DamageResult damageResult, AttackOutcome attackOutcome)
    {
        if (damageResult.IsCrit) return $"{damageResult.TotalDamage} critical";
        if (attackOutcome == AttackOutcome.Parry) return $"{damageResult.TotalDamage} parried";
        if (attackOutcome == AttackOutcome.Block) return $"{damageResult.TotalDamage} blocked";
        return damageResult.TotalDamage.ToString();
    }

    public static void SetSourceNameForStatusEffects(StatusDefinition definition, string name)
    {
        foreach (var trigger in definition.Triggers)
        {
            foreach (var effect in trigger.Actions)
                effect.SourceName = name;
        }
    }
}
