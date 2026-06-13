using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.Triggers;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Attributes;
using Domain.Models.Damages;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Combat;
public static class BasicAttackLoader
{
    public static CombatAbilityInstance LoadBasicAttack(EquipmentSlot? slot)
    {
        var magnitude = 5;
        var scalingAttribute = AttributeType.Power;
        var scalingAmount = 0.1f;

        var equipment = slot?.EquipmentInstance;
        if (equipment != null)
        {
            magnitude = 5;
            scalingAttribute = equipment.EquipmentBase.ScalingAttribute;
            scalingAmount = 0.1f;
            //magnitude = equipment.EquipmentBase.Magnitude;
            //scalingAttribute = equipment.EquipmentBase.ScalingAttribute;
            //scalingAmount = equipment.EquipmentBase.ScalingAmount;
        }
        // Find the main hand equipment
        // Create the basic attack ability instance
        var basicAttackAction = new EffectDefinition(
            new CombatEffectAction
            {
                Operation = CombatEffectOperation.Damage,
                Magnitude = magnitude,
                ScalingAttribute = scalingAttribute,
                ScalingMultiplier = scalingAmount
            },
            new NoDuration(),
            new NoCondition(),
            new NoInterval(),
            new UnlimitedUsage(),
            [],
            [],
            CombatTargeting.SingleEnemy,
            AttackType.Melee,
            DamageType.Physical)
        {
            Log = "{Actor} hit {Target} with a basic attack, dealing {Amount} damage.",
            SourceName = equipment?.EquipmentBase.Name ?? "Unarmed Basic Attack"
        };

        var abilityTrigger = new Trigger()
        {
            Actions = [basicAttackAction],
            Event = TriggerEvent.BasicAttack,
            Filters = [new SourceIsSelfFilter(null)]
        };

        var abilityDefinition = new CombatAbilityDefinition()
        {
            Triggers = [abilityTrigger],
            Type = CombatAbilityType.Passive
        };

        var basicAttackAbility = new CombatAbilityInstance(abilityDefinition);

        return basicAttackAbility;
    }
}
