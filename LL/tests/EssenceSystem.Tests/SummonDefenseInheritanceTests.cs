using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class SummonDefenseInheritanceTests
{
    public static IEnumerable<object[]> CurrentDefenseCases()
    {
        foreach (var defense in new[] { AttributeType.Armor, AttributeType.Resistance })
            foreach (var rating in new[] { 0f, 30f, 165f, 365.36816f, 5000f })
                yield return [defense, rating];
    }

    [Theory]
    [MemberData(nameof(CurrentDefenseCases))]
    public void Current_summons_inherit_uncapped_ratings_without_percentage_reconversion(AttributeType defense, float rating)
    {
        var (result, _) = Run(defense, defense, rating, AttributeRules.CurrentVersion, 0, 0, 1);
        var expected = (int)Math.Round(10000 * (1 - AttributeRules.Mitigation(Math.Round(rating))));
        AssertCopies(result, expected, 10000);
    }

    [Theory]
    [InlineData(AttributeType.Armor, 0, 0, .5f, 82)]
    [InlineData(AttributeType.Resistance, 0, 0, .5f, 82)]
    [InlineData(AttributeType.Armor, 20, 0, .5f, 138)]
    [InlineData(AttributeType.Resistance, 20, 0, .5f, 138)]
    [InlineData(AttributeType.Armor, 0, 40, .1f, 165)]
    [InlineData(AttributeType.Resistance, 0, 40, .1f, 165)]
    public void Inherited_ratings_scale_and_convert_authored_base_and_minimum_to_matching_units(
        AttributeType defense, int baseValue, int minimum, float coefficient, int expectedRating)
    {
        var (result, _) = Run(defense, defense, 165, AttributeRules.CurrentVersion, baseValue, minimum, coefficient);
        AssertCopies(result, (int)Math.Round(10000 * (1 - AttributeRules.Mitigation(expectedRating))), 10000);
    }

    [Theory]
    [InlineData(AttributeType.Armor, AttributeType.ArmorRating)]
    [InlineData(AttributeType.Resistance, AttributeType.ResistanceRating)]
    [InlineData(AttributeType.ArmorRating, AttributeType.Armor)]
    [InlineData(AttributeType.ResistanceRating, AttributeType.Resistance)]
    public void Explicit_rating_attributes_share_the_current_inheritance_unit(AttributeType target, AttributeType scaling)
    {
        var (result, _) = Run(target, scaling, 165, AttributeRules.CurrentVersion, 0, 0, 1);
        AssertCopies(result, 6000, 10000);
    }

    [Theory]
    [InlineData(AttributeType.Armor, 1f, 6000)]
    [InlineData(AttributeType.Resistance, 1f, 6000)]
    [InlineData(AttributeType.Armor, .5f, 8000)]
    [InlineData(AttributeType.Resistance, .5f, 8000)]
    public void Legacy_summons_keep_percentage_inheritance(AttributeType defense, float coefficient, int damage)
    {
        var (result, _) = Run(defense, defense, 40, AttributeRules.LegacyVersion, 0, 0, coefficient);
        AssertCopies(result, damage, 10000);
    }

    public static IEnumerable<object[]> StaticDefenseCases()
    {
        foreach (var version in new[] { AttributeRules.LegacyVersion, AttributeRules.CurrentVersion })
            foreach (var defense in new[] { AttributeType.Armor, AttributeType.Resistance })
                foreach (var source in new AttributeType?[] { null, defense, AttributeType.Power })
                    yield return [version, defense, source!];
    }

    [Theory]
    [MemberData(nameof(StaticDefenseCases))]
    public void Static_and_non_defense_scaled_authored_percentages_are_preserved(int version, AttributeType defense, AttributeType? scaling)
    {
        // A zero coefficient and a static minimum retain the original authored
        // percentage semantics, including when a scaling attribute is present.
        var (result, _) = Run(defense, scaling, 165, version, 20, 40, 0);
        AssertCopies(result, 6000, 10000);
    }

    [Theory]
    [InlineData(AttributeType.Armor)]
    [InlineData(AttributeType.Resistance)]
    public void Scaling_authored_defense_from_power_keeps_percentage_units(AttributeType defense)
    {
        var (result, _) = Run(defense, AttributeType.Power, 100, AttributeRules.CurrentVersion, 0, 0, .2f);
        AssertCopies(result, 8000, 10000);
    }

    [Theory]
    [InlineData(DamageType.Physical)]
    [InlineData(DamageType.Magical)]
    public void Floor_nine_copies_keep_ten_percent_health_and_inherit_the_guardians_rounded_defense(DamageType type)
    {
        var catalog = Catalog();
        var copy = catalog.SummonsById["niCopy"];
        Assert.Equal(.1f, copy.Attributes.Single(a => a.Attribute == AttributeType.MaxHealth).ScalingCoefficient);
        var ni = Actor("ni", CombatTeam.Friendly,
            [AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninefold"])],
            new() { [AttributeType.MaxHealth] = 11251.875f, [AttributeType.Power] = 1655.7346f,
                [AttributeType.ArmorRating] = 365.36816f, [AttributeType.ResistanceRating] = 365.36816f }, AttributeRules.CurrentVersion);
        var enemy = Actor("enemy", CombatTeam.Hostile, [Hit(type, 1000)], new() { [AttributeType.MaxHealth] = 100000 }, AttributeRules.CurrentVersion);
        var result = Engine(catalog).Run([ni], [enemy]);
        AssertCopies(result, 449, 1125);
        Assert.Equal(1655.7346f, ni.GetAttribute(AttributeType.Power));
        Assert.DoesNotContain(result.EventLog, e => e.Source == "effect.creature.ni.ninefold.power");
    }

    private static (CombatResult Result, RuntimeCombatant Owner) Run(AttributeType target, AttributeType? scaling,
        float sourceDefense, int version, int baseValue, int minimum, float coefficient)
    {
        var catalog = Catalog();
        var attributes = catalog.SummonsById["niCopy"].Attributes;
        attributes.RemoveAll(a => a.Attribute is AttributeType.Armor or AttributeType.Resistance);
        attributes.Add(new() { Attribute = target, ScalingAttribute = scaling, ScalingCoefficient = coefficient, BaseValue = baseValue, MinimumValue = minimum });
        var sourceAttribute = scaling ?? target;
        if (version == AttributeRules.CurrentVersion) sourceAttribute = AttributeRules.RatingAttribute(sourceAttribute);
        var owner = Actor("ni", CombatTeam.Friendly, [AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninefold"])],
            new() { [AttributeType.MaxHealth] = 100000, [sourceAttribute] = sourceDefense }, version);
        var type = target is AttributeType.Armor or AttributeType.ArmorRating ? DamageType.Physical : DamageType.Magical;
        var enemy = Actor("enemy", CombatTeam.Hostile, [Hit(type, 10000)], new() { [AttributeType.MaxHealth] = 100000 }, version);
        return (Engine(catalog).Run([owner], [enemy]), owner);
    }

    private static void AssertCopies(CombatResult result, int expectedDamage, int health)
    {
        var copies = result.EntityStats.Where(e => e.EntityName == "Ninefold Copy").ToArray();
        Assert.Equal(9, copies.Length);
        Assert.All(copies, copy =>
        {
            Assert.Equal(health, copy.MaxHealth);
            var hit = Assert.Single(result.EventLog.Where(e => e.TargetId == copy.EntityId && e.Source == "test.sweep" && e.EventType == EventType.Damage));
            Assert.Equal(expectedDamage, hit.Magnitude);
            Assert.DoesNotContain(result.EventLog, e => e.ActorId == copy.EntityId && e.EventType is EventType.Damage or EventType.AbilityUse);
        });
    }

    private static CompiledAbility Hit(DamageType type, int damage) => AbilityCompiler.CompileAbility(new AbilitySpec {
        Id = "test.sweep", Name = "Sweep", Kind = AbilitySpecKind.Active, Effects = [new() {
            Id = "test.sweep", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.AllEnemies,
            BaseValue = damage, DamageType = type, CritEligibility = CritEligibility.Disallowed }] });
    private static AbilityCatalog Catalog() => new JsonAbilityCatalogProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot(), HarnessJson.Options).GetCatalog();
    private static FastCombatEngine Engine(AbilityCatalog catalog) => new(AbilityCompiler.CompileStatuses(catalog.Statuses), AbilityCompiler.CompileSummons(catalog.Summons), AbilityCompiler.CompileAbilities(catalog.Abilities), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 17));
    private static RuntimeCombatant Actor(string id, CombatTeam team, IReadOnlyList<CompiledAbility> abilities, Dictionary<AttributeType, float> attributes, int version)
        => new(id, id, team, attributes, abilities, canBasicAttack: false, attributeRulesVersion: version);
}
