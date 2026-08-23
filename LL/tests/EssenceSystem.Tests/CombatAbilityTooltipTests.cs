using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Logging.Abstractions;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class CombatAbilityTooltipTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration =>
        {
            configuration.AddProfile<TestCombatStatsProfile>();
            configuration.AddProfile<EssenceAbilityMappingProfile>();
            configuration.AddProfile<EssenceEffectMappingProfile>();
        },
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Compiled_ability_retains_the_exact_source_definition()
    {
        var definition = CreateDefinition();

        var compiled = AbilityCompiler.CompileAbility(definition);

        Assert.Same(definition, compiled.SourceSpec);
    }

    [Fact]
    public void Combat_stats_map_the_ability_definition_to_essence_details()
    {
        var definition = CreateDefinition();
        var stats = new AbilityStats(definition.Name, Uses: 2, Definition: definition);

        var dto = _mapper.Map<AbilityStatsDto>(stats);

        Assert.NotNull(dto.Definition);
        Assert.Equal(definition.Id, dto.Definition.Id);
        Assert.Equal(definition.Description, dto.Definition.Description);
        Assert.Equal(4.5, dto.Definition.CooldownSeconds);
        Assert.Equal("Damage", Assert.Single(dto.Definition.Effects).Type);
    }

    [Fact]
    public void Combat_stats_leave_uncatalogued_ability_definitions_null()
    {
        var stats = new AbilityStats("Basic Attack", Uses: 1);

        var dto = _mapper.Map<AbilityStatsDto>(stats);

        Assert.Null(dto.Definition);
    }

    private static AbilitySpec CreateDefinition() => new()
    {
        Id = "ability.tooltip-test",
        Kind = AbilitySpecKind.Active,
        Name = "Tooltip Test",
        Description = "Deals {effect.damage} damage.",
        CooldownTicks = 45,
        Effects =
        [
            new AbilityEffectSpec
            {
                Id = "effect.damage",
                Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget,
                BaseValue = 12
            }
        ]
    };

    private sealed class TestCombatStatsProfile : Profile
    {
        public TestCombatStatsProfile() => new AbilityStatsDto().Mapping(this);
    }
}
