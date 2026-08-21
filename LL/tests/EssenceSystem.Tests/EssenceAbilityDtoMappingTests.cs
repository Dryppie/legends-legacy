using Application.UseCases.Essences.Dtos;
using AutoMapper;
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
