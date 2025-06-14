using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Duration;
using Domain.Models.Abilities.Effects.Intervals;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.Effects.Usages;
using Domain.Models.Abilities.Triggers;
using Domain.Models.Abilities.Triggers.TriggerFilters;
using Domain.Models.Attributes;
using Domain.Models.Damages;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Combat;
public static class BasicAttackLoader
{
    public static AbilityInstance LoadBasicAttack(EquipmentSlot? slot)
    {
        var magnitude = 5;
        var scalingAttribute = AttributeType.Strength;
        var scalingAmount = 0.1f;

        var equipment = slot?.EquipmentInstance;
        if (equipment != null)
        {
            magnitude = equipment.EquipmentBase.Magnitude;
            scalingAttribute = equipment.EquipmentBase.ScalingAttribute;
            scalingAmount = equipment.EquipmentBase.ScalingAmount;
        }
        // Find the main hand equipment
        // Create the basic attack ability instance
        var basicAttackAction = new EffectDefinition(
            new DamageAction(magnitude, scalingAttribute, scalingAmount),
            new NoDuration(),
            new NoCondition(),
            new NoInterval(),
            new UnlimitedUsage(),
            [],
            [],
            Targeting.SingleEnemy,
            AttackType.Melee,
            DamageType.Physical)
        { Log = "{Test} - {Actor} hit {Target} with a basic attack, dealing {Amount} damage." };
        basicAttackAction.Log.Replace("{Test}", scalingAttribute.ToString());

        var abilityTrigger = new Trigger()
        {
            Actions = [basicAttackAction],
            Event = TriggerEvent.BasicAttack,
            Filters = [new SourceIsSelfFilter(null)]
        };

        var abilityDefinition = new AbilityDefinition()
        {
            Triggers = [abilityTrigger],
            Type = AbilityType.Passive
        };

        var basicAttackAbility = new AbilityInstance(abilityDefinition);

        return basicAttackAbility;
    }
}
