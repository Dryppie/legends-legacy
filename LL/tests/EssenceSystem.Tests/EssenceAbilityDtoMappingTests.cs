using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class EssenceAbilityDtoMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration =>
        {
            configuration.AddProfile<EssenceAbilityMappingProfile>();
            configuration.AddProfile<EssenceEffectMappingProfile>();
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
}
