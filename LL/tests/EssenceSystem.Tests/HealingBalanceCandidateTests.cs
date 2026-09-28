using System.Text.Json;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class HealingBalanceCandidateTests
{
    private const string Herb = "ability.creature.lizardfolk_shaman.herb_mixture";
    private const string Sapling = "ability.creature.treant_sapling.sprouting_surge";

    private static JsonAbilityCatalogProvider Provider(string? profile = null, int version = 18) => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Combat:AbilityBalanceProfile"] = profile,
            ["AttributeRedesign:LiveVersion"] = version.ToString()
        }).Build(), TestContentPaths.FindApiRoot(), HarnessJson.Options);

    [Fact]
    public void Opt_in_profile_changes_only_the_two_healing_coefficients()
    {
        var baseline = Provider().GetCatalog();
        var candidate = Provider("healing-v1").GetCatalog();
        Assert.Equal(baseline.Abilities.Count, candidate.Abilities.Count);
        foreach (var original in baseline.Abilities)
        {
            var replacement = candidate.AbilitiesById[original.Id];
            if (original.Id is Herb or Sapling)
            {
                Assert.Contains("{scaling}", replacement.Description);
                var effect = Assert.Single(replacement.Effects);
                Assert.Equal(original.Effects[0].ScalingCoefficient / 2, effect.ScalingCoefficient);
                effect.ScalingCoefficient *= 2;
            }
            Assert.Equal(JsonSerializer.Serialize(original, HarnessJson.Options), JsonSerializer.Serialize(replacement, HarnessJson.Options));
        }
        // Loading a candidate must never mutate another provider's baseline.
        Assert.Equal(2.1f, Provider().GetCatalog().AbilitiesById[Herb].Effects[0].ScalingCoefficient);
        Assert.Equal(2.5f, Provider().GetCatalog().AbilitiesById[Sapling].Effects[0].ScalingCoefficient);
    }

    [Theory]
    [InlineData(Herb, 0, 0)]
    [InlineData(Herb, 50, 0)]
    [InlineData(Herb, 100, 0)]
    [InlineData(Herb, 200, 3)]
    [InlineData(Sapling, 0, 0)]
    [InlineData(Sapling, 50, 0)]
    [InlineData(Sapling, 100, 0)]
    [InlineData(Sapling, 200, 3)]
    public void Authored_candidate_heals_use_restoration_and_ascension(string id, int restoration, int ascension)
    {
        var ability = EssenceAbilityProgressionScaler.Apply(Provider("healing-v1").GetCatalog().AbilitiesById[id], ascension);
        var baseCoefficient = id == Herb ? 1.05 : 1.25;
        Assert.Equal(baseCoefficient * (1 + .1 * ascension), ability.Effects[0].ScalingCoefficient, 5);
        var zeroRestoration = Heal(0);
        // Magnitudes roll +/-20% before Restoration; paired seeds isolate the multiplier.
        Assert.InRange(zeroRestoration, 80 * ability.Effects[0].ScalingCoefficient - 1, 120 * ability.Effects[0].ScalingCoefficient + 1);
        var expected = zeroRestoration * (1 + restoration / 100d);
        Assert.InRange(Heal(restoration), expected - 1, expected + 1);
        Assert.Equal(ascension == 0 ? (id == Herb ? 140 : 160) : (id == Herb ? 119 : 136), ability.CooldownTicks);

        float Heal(int points)
        {
            var actor = Actor("actor", CombatTeam.Friendly, [ability], points);
            actor.SetHealth(100);
            new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, RandomSeed: 41))
                .Run([actor], [Actor("enemy", CombatTeam.Hostile, [], 0)]);
            return actor.Health - 100;
        }
    }

    [Fact]
    public void Restoration_price_doubles_specialization_points_without_changing_budgets_or_other_prices()
    {
        var releases = new JsonEquipmentCatalogProvider(Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));
        var oldBalance = releases.Get(3).Evaluator.Balance;
        var newBalance = releases.Get(4).Evaluator.Balance;
        Assert.Equal(3, oldBalance.GetMaterializedCostPerPoint(AttributeType.Restoration, 1));
        Assert.Equal(1.5, newBalance.GetMaterializedCostPerPoint(AttributeType.Restoration, 1));
        foreach (var (attribute, cost) in oldBalance.AttributeCosts.Where(x => x.Key != AttributeType.Restoration))
            Assert.Equal(cost, newBalance.AttributeCosts[attribute]);
        foreach (var id in new[] { "plain.band.spec.restoration.rarity.common", "plain.staff.spec.restoration.rarity.epic" })
        {
            var before = releases.Get(3).Evaluator.Evaluate(id, 1, 0, null);
            var after = releases.Get(4).Evaluator.Evaluate(id, 1, 0, null);
            Assert.Equal(before.TargetBudget, after.TargetBudget);
            Assert.Equal(before.Allocation, after.Allocation);
            Assert.Equal(before.Stats[AttributeType.Restoration] * 2, after.Stats[AttributeType.Restoration]);
        }
    }

    [Theory]
    [InlineData("../healing-v1", 18)]
    [InlineData("healing-v1", 17)]
    public void Profile_rejects_unsafe_paths_and_incompatible_combat_rules(string profile, int version) =>
        Assert.Throws<InvalidOperationException>(() => Provider(profile, version));

    private static RuntimeCombatant Actor(string id, CombatTeam team, AbilitySpec[] abilities, int restoration) => new(
        id, id, team, new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 10000, [AttributeType.Power] = 100, [AttributeType.Restoration] = restoration
        }, abilities.Select(AbilityCompiler.CompileAbility), canBasicAttack: false, attributeRulesVersion: 18);
}
