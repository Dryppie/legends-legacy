using Domain.Models.Attributes;
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
    public void Validate_accepts_content_facing_stat_system_attributes()
    {
        var definition = ValidDefinition();
        definition.AttributeBonuses.Add(new EssenceAttributeBonusDefinition { Attribute = AttributeType.Power, BaseValue = 2 });
        definition.AttributeBonuses.Add(new EssenceAttributeBonusDefinition { Attribute = AttributeType.MaxHealth, BaseValue = 5 });
        definition.AttributeBonuses.Add(new EssenceAttributeBonusDefinition { Attribute = AttributeType.SummonPower, BaseValue = 3 });

        var errors = _validator.Validate([definition]);

        Assert.DoesNotContain(errors, error => error.Contains("attribute", StringComparison.OrdinalIgnoreCase));
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
            var dataPath = Path.Combine(directory.FullName, "src", "API", "API.LL", "Data");
            var essenceCandidate = Path.Combine(dataPath, "essences.json");
            var abilityCandidate = Path.Combine(dataPath, "abilities.json");
            if (File.Exists(essenceCandidate) && File.Exists(abilityCandidate))
                return Path.Combine(directory.FullName, "src", "API", "API.LL");

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/essences.json and abilities.json from test output directory.");
    }
}
