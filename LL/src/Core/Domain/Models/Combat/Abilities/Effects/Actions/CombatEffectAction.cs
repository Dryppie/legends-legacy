using Domain.Interfaces.Combat;
using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities.ResourceCosts;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class CombatEffectAction : IEffectAction
{
    public string Operation { get; init; } = CombatEffectOperation.Damage;
    public int Magnitude { get; init; }
    public ResourceType? Resource { get; init; }
    public AttributeType? Attribute { get; init; }
    public ModifierType ModifierType { get; init; } = ModifierType.Flat;
    public bool Stackable { get; init; }
    public string? StatusId { get; init; }
    public int StatusDuration { get; init; }
    public string? SecondaryEffectId { get; init; }
    public string? SummonId { get; init; }
    public int SummonDuration { get; init; }
    public float SummonPowerMultiplier { get; init; } = 1;
    public float SummonHealthMultiplier { get; init; } = 1;
    public AttributeType? ScalingAttribute { get; init; }
    public float ScalingMultiplier { get; init; }
    public float LifeStealPercentage { get; init; }

    public void Execute(EffectContext effect, ICombatContext combatContext) =>
        CombatEffectActionDispatcher.Execute(this, effect, combatContext);

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext) =>
        CombatEffectActionDispatcher.OnExpire(this, effect, combatContext);
}
