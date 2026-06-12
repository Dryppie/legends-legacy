using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences.Definitions;
using Domain.Models.Attributes;
using Services.LL.Essences;
using AuthoredAbilityDefinition = Domain.Models.AbilityDefinitions.AbilityDefinition;

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
    public void Validate_rejects_unknown_effect_types()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Effects[0].Type = "InventNewEffect";

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("unknown effect type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_unknown_targets()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Targeting = "BackPocket";

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("unknown target selector", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_active_and_passive_ability_kind_mismatches()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Kind = AbilityDefinitionKind.Passive;
        definition.PassiveAbility.Kind = AbilityDefinitionKind.Active;

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("must reference an Active ability definition", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("must reference a Passive ability definition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_unknown_conditions()
    {
        var definition = ValidDefinition();
        definition.PassiveAbility.Conditions.Add(new AbilityConditionDefinition { Type = "OnlyOnTuesdays" });

        var errors = _validator.Validate([definition]);

        Assert.Contains(errors, error => error.Contains("unknown condition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_unknown_condition_tags()
    {
        var definition = ValidDefinition();
        definition.ActiveAbility.Effects[0].Conditions.Add(new AbilityConditionDefinition { Type = AbilityConditionType.TargetHasTag, Tag = "Role.Unknown" });

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

    internal static EssenceDefinition ValidDefinition() => new()
    {
        Id = "essence.test",
        SourceMonsterId = "monster.test",
        Name = "Test Essence",
        ActiveAbilityId = "active.test",
        PassiveAbilityId = "passive.test",
        Tags = ["Species.Beast", "Role.Offensive"],
        ActiveAbility = new AuthoredAbilityDefinition
        {
            Id = "active.test",
            Kind = AbilityDefinitionKind.Active,
            Tags = ["Effect.Ability"],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Type = "Damage"
                }
            ]
        },
        PassiveAbility = new AuthoredAbilityDefinition
        {
            Id = "passive.test",
            Kind = AbilityDefinitionKind.Passive,
            Tags = ["Trigger.OnHit"],
            Effects =
            [
                new()
                {
                    Id = "effect.attribute.main",
                    Type = "ModifyAttribute",
                    Attribute = "Power"
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
}
