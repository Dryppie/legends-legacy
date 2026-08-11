using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace EssenceSystem.Tests;

public sealed class EssenceAbilityProgressionScalerTests
{
    [Fact]
    public void Apply_scales_every_combat_coefficient_and_cooldown_without_mutating_the_definition()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.scaling",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 200,
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnHit,
                    InternalCooldownTicks = 100
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.damage",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 100,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1.2f,
                    MaximumScalingCoefficient = 2f,
                    EventMagnitudeCoefficient = 0.5f,
                    ConditionScalingCoefficient = 0.25f,
                    StatusScalingCoefficient = 0.1f
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, ascensionTier: 1);
        var effect = Assert.Single(scaled.Effects);

        Assert.NotSame(ability, scaled);
        Assert.Equal(190, scaled.CooldownTicks);
        Assert.Equal(95, Assert.Single(scaled.Triggers).InternalCooldownTicks);
        Assert.Equal(112, effect.BaseValue);
        Assert.Equal(1.344f, effect.ScalingCoefficient, precision: 3);
        Assert.Equal(2.24f, effect.MaximumScalingCoefficient, precision: 3);
        Assert.Equal(0.56f, effect.EventMagnitudeCoefficient, precision: 3);
        Assert.Equal(0.28f, effect.ConditionScalingCoefficient, precision: 3);
        Assert.Equal(0.112f, effect.StatusScalingCoefficient, precision: 3);

        Assert.Equal(200, ability.CooldownTicks);
        Assert.Equal(100, ability.Triggers.Single().InternalCooldownTicks);
        Assert.Equal(100, ability.Effects.Single().BaseValue);
        Assert.Equal(1.2f, ability.Effects.Single().ScalingCoefficient);
    }

    [Fact]
    public void Apply_scales_utility_modifiers_and_summon_stats_by_their_profiles()
    {
        var ability = new AbilitySpec
        {
            Kind = AbilitySpecKind.Passive,
            Description = "Take 10% less damage. Every third attack deals 30% increased damage.",
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "effect.damage-taken",
                    Operation = AbilityEffectOperation.ModifyDamageTaken,
                    BaseValue = -10
                },
                new AbilityEffectSpec
                {
                    Id = "effect.next-hit",
                    Operation = AbilityEffectOperation.ModifyNextBasicAttackDamage,
                    BaseValue = 30
                },
                new AbilityEffectSpec
                {
                    Id = "effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    SummonPowerMultiplier = 1,
                    SummonHealthMultiplier = 1
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, ascensionTier: 1);

        Assert.Equal(-11, scaled.Effects[0].BaseValue);
        Assert.Equal(34, scaled.Effects[1].BaseValue);
        Assert.Equal(
            "Take 11% less damage. Every third attack deals 34% increased damage.",
            scaled.Description);
        Assert.Equal(
            "Take 10% less damage. Every third attack deals 30% increased damage.",
            ability.Description);
        Assert.Equal(1.12, scaled.Effects[2].SummonPowerMultiplier, precision: 3);
        Assert.Equal(1.12, scaled.Effects[2].SummonHealthMultiplier, precision: 3);
    }
}
