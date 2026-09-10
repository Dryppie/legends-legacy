using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Logging.Abstractions;
using Services.LL.Combat.Engine;
using Services.LL.CombatStyles;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class ChanneledEssenceResolverTests
{
    [Theory]
    [InlineData(AbilityEffectOperation.Damage)]
    [InlineData(AbilityEffectOperation.Heal)]
    [InlineData(AbilityEffectOperation.GrantBarrier)]
    public void Direct_active_effects_are_channeled_eligible(AbilityEffectOperation operation)
    {
        var definition = Definition(new AbilityEffectSpec { Id = "direct", Operation = operation, BaseValue = 10 });
        var result = Resolver(definition).Resolve(Essence(definition));

        Assert.True(result.IsEligible);
        Assert.Equal(["direct"], result.EligibleEffectIds);
    }

    [Fact]
    public void Periodic_secondary_and_damage_derived_effects_do_not_make_an_active_channeled_eligible()
    {
        var definition = Definition(
            new AbilityEffectSpec { Id = "periodic", Operation = AbilityEffectOperation.Damage, BaseValue = 10, DurationTicks = 20, IntervalTicks = 10 },
            new AbilityEffectSpec { Id = "secondary", Operation = AbilityEffectOperation.Damage, BaseValue = 10, Tags = ["Damage.Secondary"] },
            new AbilityEffectSpec { Id = "derived", Operation = AbilityEffectOperation.Heal, EventMagnitudeCoefficient = 1 });

        var result = Resolver(definition).Resolve(Essence(definition));

        Assert.False(result.IsEligible);
        Assert.Empty(result.EligibleEffectIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Archive_flag_uses_current_evolved_ability_and_matches_combat_channeled_resolution(bool evolved)
    {
        var definition = Definition(new AbilityEffectSpec { Id = "utility", Operation = AbilityEffectOperation.Cleanse });
        definition.Evolution.ActiveAbilityModifiers = [new()
        {
            Operation = "AddEffect", Target = "utility",
            Effect = new() { Id = "evolved-heal", Operation = AbilityEffectOperation.Heal, BaseValue = 10 }
        }];
        var repository = new Definitions(definition);
        var resolver = new ChanneledEssenceResolver(repository, new Catalog(repository.GetAllAbilities()));
        var essence = Essence(definition);
        essence.IsEvolved = evolved;
        essence.AscensionTier = 1;
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile<SoulArchiveMappingProfile>();
            config.AddProfile<EssenceAbilityMappingProfile>();
            config.AddProfile<EssenceEffectMappingProfile>();
        }, NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper(type => type == typeof(PlayerEssenceArchiveEntryConverter)
            ? new PlayerEssenceArchiveEntryConverter(repository, new EssenceProgressionService(), resolver)
            : Activator.CreateInstance(type)!);

        var option = resolver.Resolve(essence);
        var dto = mapper.Map<PlayerEssenceDto>(new PlayerEssenceArchiveEntry(essence, 2));

        Assert.Equal(evolved, option.IsEligible);
        Assert.Equal(option.IsEligible, dto.IsChanneledEssenceEligible);
        Assert.Equal(2, dto.AttunedSlot);
        Assert.Equal(evolved ? ["evolved-heal"] : Array.Empty<string>(), option.EligibleEffectIds);
        Assert.Single(definition.ActiveAbility.Effects); // Preparation must not mutate the shared definition.
    }

    [Fact]
    public void Missing_definition_is_not_channeled_eligible()
    {
        var definition = Definition(new AbilityEffectSpec { Id = "direct", Operation = AbilityEffectOperation.Damage });
        var essence = Essence(definition);
        essence.EssenceDefinitionId = "missing";

        var result = Resolver(definition).Resolve(essence);

        Assert.False(result.IsEligible);
        Assert.Empty(result.EligibleEffectIds);
    }

    private static ChanneledEssenceResolver Resolver(EssenceDefinition definition)
    {
        var repository = new Definitions(definition);
        return new(repository, new Catalog(repository.GetAllAbilities()));
    }

    private static PlayerEssence Essence(EssenceDefinition definition) => new()
    {
        Id = Guid.NewGuid(), EssenceDefinitionId = definition.Id, Level = 1
    };

    private static EssenceDefinition Definition(params AbilityEffectSpec[] effects) => new()
    {
        Id = "channeled-test", Name = "Channeled test", ActiveAbilityId = "active", PassiveAbilityId = "passive",
        ActiveAbility = new() { Id = "active", Name = "Active", Kind = AbilitySpecKind.Active, Effects = [.. effects] },
        PassiveAbility = new()
        {
            Id = "passive", Name = "Passive", Kind = AbilitySpecKind.Passive,
            Triggers = [new() { Event = AbilityTriggerEvent.OnHit }],
            Effects = [new() { Id = "passive-damage", Operation = AbilityEffectOperation.Damage, BaseValue = 10 }]
        },
        Evolution = new() { Id = "evolution", RequiredAscensionTier = 1 }
    };

    private sealed class Definitions(EssenceDefinition definition) : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [definition];
        public EssenceDefinition? GetById(string id) => id == definition.Id ? definition : null;
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [definition.ActiveAbility, definition.PassiveAbility];
        public AbilitySpec? GetAbilityById(string id) => GetAllAbilities().FirstOrDefault(x => x.Id == id);
    }

    private sealed class Catalog(IReadOnlyList<AbilitySpec> abilities) : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => new(abilities, [], [], new Dictionary<string, string>());
    }
}
