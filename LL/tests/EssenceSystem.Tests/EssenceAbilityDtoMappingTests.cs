using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class EssenceAbilityDtoMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration =>
        {
            configuration.AddProfile<EssenceAbilityMappingProfile>();
            configuration.AddProfile<EssenceEffectMappingProfile>();
            configuration.AddProfile<EssenceEvolutionMappingProfile>();
            configuration.AddProfile<TestEssenceDefinitionProfile>();
        },
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Ability_mapping_exposes_every_distinct_target_in_effect_order()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.multi-target",
            Kind = AbilitySpecKind.Active,
            Name = "Multi Target",
            Effects =
            [
                new()
                {
                    Id = "effect.enemy.one",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget
                },
                new()
                {
                    Id = "effect.self",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self
                },
                new()
                {
                    Id = "effect.enemy.two",
                    Operation = AbilityEffectOperation.ApplyCondition,
                    Target = AbilityTargetSelector.CurrentTarget
                }
            ]
        };

        var dto = _mapper.Map<EssenceAbilityDto>(ability);

        Assert.Equal(["CurrentTarget", "Self"], dto.Targets);
    }

    [Fact]
    public void Ability_without_effects_does_not_claim_a_default_target()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.no-target",
            Kind = AbilitySpecKind.Passive,
            Name = "No Target"
        };

        var dto = _mapper.Map<EssenceAbilityDto>(ability);

        Assert.Empty(dto.Targets);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Ability_mapping_exposes_description_values_at_every_ascension_tier(int tier)
    {
        var ability = new AbilitySpec
        {
            Id = "ability.scaled-description",
            Kind = AbilitySpecKind.Passive,
            Name = "Scaled Description",
            Effects =
            [
                new()
                {
                    Id = "effect.event",
                    Operation = AbilityEffectOperation.Heal,
                    EventMagnitudeCoefficient = 0.05f
                },
                new()
                {
                    Id = "effect.condition",
                    Operation = AbilityEffectOperation.Damage,
                    ConditionScalingCoefficient = 0.02f
                },
                new()
                {
                    Id = "effect.status",
                    Operation = AbilityEffectOperation.Damage,
                    StatusScalingCoefficient = 0.2f
                },
                new()
                {
                    Id = "effect.duration",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    DurationTicks = 60
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, tier);
        var dto = _mapper.Map<EssenceAbilityDto>(scaled);

        Assert.Equal(scaled.Effects[0].EventMagnitudeCoefficient, dto.Effects[0].EventMagnitudeCoefficient, precision: 6);
        Assert.Equal(scaled.Effects[1].ConditionScalingCoefficient, dto.Effects[1].ConditionScalingCoefficient, precision: 6);
        Assert.Equal(scaled.Effects[2].StatusScalingCoefficient, dto.Effects[2].StatusScalingCoefficient, precision: 6);
        Assert.Equal(scaled.Effects[3].DurationTicks / 10d, dto.Effects[3].DurationSeconds);
    }

    [Fact]
    public void Ability_mapping_exposes_ascension_scaled_summon_stat_multipliers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.summon-description",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Description",
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    SummonPowerMultiplier = 1,
                    SummonHealthMultiplier = 1
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, 1);
        var dto = _mapper.Map<EssenceAbilityDto>(scaled);
        var effect = Assert.Single(dto.Effects);

        Assert.Equal(1.12, effect.SummonPowerMultiplier, precision: 3);
        Assert.Equal(1.12, effect.SummonHealthMultiplier, precision: 3);
    }

    [Fact]
    public void Ability_mapping_exposes_percentage_of_initial_attribute_scaling_for_descriptions()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.initial-attribute-description",
            Kind = AbilitySpecKind.Passive,
            Name = "Initial Attribute Description",
            Effects =
            [
                new()
                {
                    Id = "effect.armor",
                    Operation = AbilityEffectOperation.ModifyAttributePercentOfInitial,
                    Attribute = AttributeType.Armor,
                    ScalingCoefficient = 0.3f
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, 1);
        var dto = _mapper.Map<EssenceAbilityDto>(scaled);
        var scaling = Assert.Single(Assert.Single(dto.Effects).Scaling);

        Assert.Equal(AttributeType.Armor.ToString(), scaling.Attribute);
        Assert.Equal(0.324, scaling.Coefficient, precision: 3);
    }

    [Theory]
    [InlineData(0, "Can occur once every 3 seconds.")]
    [InlineData(1, "Can occur once every 2.8 seconds.")]
    [InlineData(2, "Can occur once every 2.7 seconds.")]
    [InlineData(3, "Can occur once every 2.6 seconds.")]
    public void Ability_mapping_resolves_the_ascension_scaled_trigger_cooldown_in_the_description(
        int tier,
        string expectedDescription)
    {
        var ability = new AbilitySpec
        {
            Id = "ability.trigger-cooldown-description",
            Kind = AbilitySpecKind.Passive,
            Name = "Trigger Cooldown Description",
            Description = "Can occur once every {triggerCooldown}.",
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnDamageDealt,
                    InternalCooldownTicks = 30
                }
            ]
        };

        var scaled = EssenceAbilityProgressionScaler.Apply(ability, tier);
        var dto = _mapper.Map<EssenceAbilityDto>(scaled);

        Assert.Equal(expectedDescription, dto.Description);
    }

    [Fact]
    public void Equipped_essence_definition_uses_the_players_ascension_tier()
    {
        var definition = new EssenceDefinition
        {
            Id = "essence.goblin",
            Name = "Goblin Essence",
            ActiveAbility = new AbilitySpec
            {
                Id = "ability.shiv-jab",
                Kind = AbilitySpecKind.Active,
                Name = "Shiv Jab",
                CooldownTicks = 90,
                Effects =
                [
                    new AbilityEffectSpec
                    {
                        Id = "effect.shiv-jab.damage",
                        Operation = AbilityEffectOperation.Damage,
                        Target = AbilityTargetSelector.RandomEnemy,
                        BaseValue = 100
                    }
                ]
            },
            PassiveAbility = new AbilitySpec
            {
                Id = "ability.gobber-trapper",
                Kind = AbilitySpecKind.Passive,
                Name = "Gobber Trapper"
            }
        };
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = definition.Id,
            AscensionTier = 1
        };

        var result = PlayerEssenceDefinitionDtoMapper.Map(definition, essence, _mapper);

        Assert.Equal(8.5d, result.ActiveAbility.CooldownSeconds);
        Assert.Equal(112, result.ActiveAbility.Effects.Single().CurrentValue);
        Assert.Equal(13, result.ActiveAbility.ThreatValue);
        Assert.Equal(90, definition.ActiveAbility.CooldownTicks);
        Assert.Equal(100, definition.ActiveAbility.Effects.Single().BaseValue);
    }

    private sealed class TestEssenceDefinitionProfile : Profile
    {
        public TestEssenceDefinitionProfile()
        {
            new EssenceDefinitionDto().Mapping(this);
        }
    }
}
