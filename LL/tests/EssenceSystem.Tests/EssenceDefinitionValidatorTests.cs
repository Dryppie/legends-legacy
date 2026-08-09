using Domain.Models.Attributes;
using Domain.Models.Bonuses;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using Services.LL.Essences;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class EssenceDefinitionValidatorTests
{
    private readonly EssenceDefinitionValidator _validator = new();

    [Fact]
    public void Validate_rejects_unknown_tags()
    {
        var definition = ValidDefinition();
        definition.Tags.Add("Species.UnknownThing");

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("unknown tag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_duplicate_definition_ids()
    {
        var first = ValidDefinition();
        var second = ValidDefinition();

        var errors = _validator.Validate([first, second]);

        Assert.Contains(errors, error => error.Contains("Duplicate Essence id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_missing_evolution()
    {
        var definition = ValidDefinition();
        definition.Evolution.Id = string.Empty;

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("exactly one evolution", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_active_and_passive_ability_kind_mismatches()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Kind = AbilitySpecKind.Passive;
        definition.PassiveAbility.Kind = AbilitySpecKind.Active;

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("must reference an Active ability definition", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("must reference a Passive ability definition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_unknown_condition_tags()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Effects[0].Conditions.Add(new AbilityConditionSpec { Type = AbilityConditionType.HasTag, Tag = "Role.Unknown" });

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("requires a known tag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_legacy_essence_attribute_bonuses()
    {
        var definition = ValidDefinition();
        definition.AttributeBonuses.Add(new EssenceAttributeBonusDefinition { Attribute = AttributeType.Power, BaseValue = 2 });
        definition.Evolution.AttributeModifierChanges.Add(new EssenceAttributeBonusDefinition { Attribute = AttributeType.MaxHealth, BaseValue = 5 });

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("attribute bonuses are no longer supported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_active_ability_without_cooldown()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.CooldownTicks = 0;

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("active ability cooldown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_invalid_chance_condition_values()
    {
        var definition = ValidDefinition();
        definition.PassiveAbility.Triggers[0].Conditions.Add(new AbilityConditionSpec { Type = AbilityConditionType.ChancePercent, Value = 150 });

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("requires a value from 0 to 100", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_status_stack_conditions_without_status_or_stack_value()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Effects[0].Conditions.Add(new AbilityConditionSpec { Type = AbilityConditionType.StatusStacksAtLeast, StatusId = "Cold" });

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("requires status and a positive stack value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Authored_essence_json_passes_definition_validation()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();

        var repository = new JsonEssenceDefinitionRepository(
            config,
            FindApiContentRoot(),
            options,
            _validator);

        Assert.NotEmpty(repository.GetAll());
        Assert.All(repository.GetAll(), definition =>
        {
            Assert.Empty(definition.AttributeBonuses);
            Assert.Empty(definition.Evolution.AttributeModifierChanges);
        });
    }

    [Fact]
    public void Authored_essence_actives_use_varied_cooldown_cadences()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();

        var repository = new JsonEssenceDefinitionRepository(
            config,
            FindApiContentRoot(),
            options,
            _validator);

        var cooldowns = repository.GetAll()
            .Select(definition => definition.ActiveAbility.CooldownTicks)
            .ToArray();

        Assert.Equal(53, cooldowns.Length);
        Assert.All(cooldowns, cooldown => Assert.InRange(cooldown, 75, 220));
        Assert.True(cooldowns.Distinct().Count() >= 10);
        Assert.Contains(cooldowns, cooldown => cooldown < 100);
        Assert.Contains(cooldowns, cooldown => cooldown > 100);
    }

    [Fact]
    public void Authored_essence_codex_collection_json_passes_definition_validation()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var contentRoot = FindApiContentRoot();
        var repository = new JsonEssenceDefinitionRepository(
            config,
            contentRoot,
            options,
            _validator);

        var provider = new JsonEssenceCodexCollectionDefinitionProvider(
            config,
            contentRoot,
            options,
            repository);

        var collections = provider.GetAll();
        var regionOneEssences = repository.GetAll()
            .Select(essence => essence.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var collectedEssences = collections
            .SelectMany(collection => collection.EssenceDefinitionIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(13, collections.Count);
        Assert.All(collections, collection =>
        {
            Assert.InRange(collection.EssenceDefinitionIds.Count, 2, 6);
        });
        Assert.Equal(
            ["Creature Families", "Essence Affinities", "Regional Ecologies"],
            collections.Select(collection => collection.Category).Distinct().Order().ToArray());
        Assert.Equal(53, regionOneEssences.Count);
        Assert.Equal(regionOneEssences.Order(StringComparer.OrdinalIgnoreCase), collectedEssences.Order(StringComparer.OrdinalIgnoreCase));
        var allowedBonusKinds = new HashSet<BonusKind>
        {
            BonusKind.EssencePityProgressionGainBps,
            BonusKind.EssenceDropRateRelativeBps,
            BonusKind.EssenceExperienceGainBps,
            BonusKind.FocusedMonsterEssenceDropRateRelativeBps
        };
        Assert.All(collections, collection => Assert.Contains(collection.Bonus.Kind, allowedBonusKinds));
        Assert.Equal(
            allowedBonusKinds.Order(),
            collections.Select(collection => collection.Bonus.Kind).Distinct().Order());
    }

    internal static EssenceDefinition ValidDefinition() => new()
    {
        Id = "essence.test",
        SourceMonsterId = "monster.test",
        Name = "Test Essence",
        ActiveAbilityId = "active.test",
        PassiveAbilityId = "passive.test",
        Tags = ["Species.Beast", "Role.Offensive"],
        ActiveAbility = new AbilitySpec
        {
            Id = "active.test",
            Name = "Active Test",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 100,
            Tags = ["Effect.Ability"],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 1
                }
            ]
        },
        PassiveAbility = new AbilitySpec
        {
            Id = "passive.test",
            Name = "Passive Test",
            Kind = AbilitySpecKind.Passive,
            Tags = ["Trigger.OnHit"],
            Triggers = [new() { Event = AbilityTriggerEvent.OnHit }],
            Effects =
            [
                new()
                {
                    Id = "effect.attribute.main",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Attribute = AttributeType.Power
                }
            ]
        },
        Evolution = new EssenceEvolutionDefinition
        {
            Id = "evolution.test",
            RequiredAscensionTier = 2,
            RequiredCatalystItemId = "item.evolution_catalyst.test",
            AddsTags = ["Mechanic.Execute"]
        }
    };

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var apiPath in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                var dataPath = Path.Combine(apiPath, "Data");
                var essenceCandidate = Path.Combine(dataPath, "essences", "essences.json");
                var abilityCandidate = Path.Combine(dataPath, "combat", "abilities.json");
                if (File.Exists(essenceCandidate) && File.Exists(abilityCandidate))
                    return apiPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/essences/essences.json and combat/abilities.json from test output directory.");
    }
}
