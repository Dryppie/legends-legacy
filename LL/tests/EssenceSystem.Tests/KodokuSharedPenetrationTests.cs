using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class KodokuSharedPenetrationTests
{
    private static AbilityCatalog Catalog() => new JsonAbilityCatalogProvider(
        new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
        TestContentPaths.FindApiRoot(), HarnessJson.Options).GetCatalog();

    internal static void EnsureCandidatePenetration(AbilityCatalog catalog)
    {
        var attributes = catalog.SummonsById["venomSpawn"].Attributes;
        if (attributes.All(a => a.Attribute != AttributeType.ArmorPenetration))
            attributes.Add(new() { Attribute = AttributeType.ArmorPenetration,
                ScalingAttribute = AttributeType.ArmorPenetration, ScalingCoefficient = 1 });
        var inherited = Assert.Single(attributes.Where(a => a.Attribute == AttributeType.ArmorPenetration));
        Assert.Equal(AttributeType.ArmorPenetration, inherited.ScalingAttribute);
        Assert.Equal(1, inherited.ScalingCoefficient);
        Assert.Equal(0, inherited.BaseValue);
        Assert.Equal(0, inherited.MinimumValue);
    }

    [Fact]
    public void Candidate_penetration_fixture_supports_original_and_applied_catalogs_without_duplicates()
    {
        var catalog = Catalog();
        EnsureCandidatePenetration(catalog);
        EnsureCandidatePenetration(catalog);
    }

    [Theory]
    [InlineData(false, 1.008f, .59f)]
    [InlineData(false, 39.5f, .2f)]
    [InlineData(false, 40.32f, .2f)]
    [InlineData(false, 60f, .2f)]
    [InlineData(true, 1.008f, .59f)]
    [InlineData(true, 39.5f, .2f)]
    [InlineData(true, 40.32f, .2f)]
    [InlineData(true, 60f, .2f)]
    public void Initial_and_insect_jar_summons_inherit_rounded_penetration_with_current_cap(
        bool initial, float penetration, float expectedMitigation)
    {
        var catalog = Catalog();
        EnsureCandidatePenetration(catalog);
        var ability = catalog.AbilitiesById[initial ? "ability.creature.kodoku.survivors_struggle" : "ability.creature.kodoku.insect_jar"];
        var owner = Actor("owner", CombatTeam.Friendly, [AbilityCompiler.CompileAbility(ability)],
            new() { [AttributeType.Power] = 1000, [AttributeType.ArmorPenetration] = penetration });
        var target = Actor("target", CombatTeam.Hostile, [], new() { [AttributeType.Armor] = 60 });
        var result = new FastCombatEngine(AbilityCompiler.CompileStatuses(catalog.Statuses),
            AbilityCompiler.CompileSummons(catalog.Summons), AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 2, BasicAttackIntervalTicks: 1, RandomSeed: 17)).Run([owner], [target]);
        var spawns = result.EntityStats.Where(x => x.EntityName == "Venomspawn").ToArray();
        Assert.Equal(initial ? 5 : 3, spawns.Length);
        Assert.All(spawns, spawn =>
        {
            Assert.Equal(800, spawn.MaxHealth);
            var hits = result.EventLog.Where(e => e.ActorId == spawn.EntityId && e.EventType == EventType.Damage).ToArray();
            Assert.NotEmpty(hits);
            Assert.All(hits, hit =>
            {
                // Basic attacks roll their raw damage; defense must oppose that roll.
                Assert.InRange(hit.IncomingRawDamage, 60, 100);
                Assert.Equal((int)Math.Round(hit.IncomingRawDamage * (1 - expectedMitigation)), hit.Magnitude);
            });
        });
    }

    [Theory]
    [InlineData(DamageType.Physical)]
    [InlineData(DamageType.Magical)]
    [InlineData(DamageType.Poison)]
    public void Guardian_typed_penetration_caps_after_mitigation_for_physical_magical_and_poison(DamageType type)
    {
        var hit = AbilityCompiler.CompileAbility(new AbilitySpec { Id = "hit", Name = "Hit", Kind = AbilitySpecKind.Active,
            CooldownTicks = 1000, Effects = [new() { Id = "damage", Operation = AbilityEffectOperation.Damage,
                Target = AbilityTargetSelector.CurrentTarget, BaseValue = 100, DamageType = type, CritEligibility = CritEligibility.Disallowed }] });
        var actor = Actor("actor", CombatTeam.Friendly, [hit], new() {
            [AttributeType.ArmorPenetration] = 40.32f, [AttributeType.MagicPenetration] = 40.32f });
        var target = Actor("target", CombatTeam.Hostile, [], new() {
            [AttributeType.Armor] = 60, [AttributeType.Resistance] = 60, [AttributeType.DamageReduction] = 25 });
        new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1)).Run([actor], [target]);
        Assert.Equal(9940, target.Health);
    }

    private static RuntimeCombatant Actor(string id, CombatTeam team, IReadOnlyList<CompiledAbility> abilities,
        Dictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = 10000;
        return new RuntimeCombatant(id, id, team, attributes, abilities, canBasicAttack: false,
            attributeRulesVersion: AttributeRules.CurrentVersion);
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_KODOKU_PREPARATION")))
                Skip = "Requires explicit archived family and isolated candidate; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Archived_family_prepares_only_the_three_declared_guardian_attribute_changes()
    {
        var request = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_KODOKU_PREPARATION")!);
        var source = request.GetProperty("source").GetString()!;
        var candidate = request.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var beforeRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var afterRunner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var cells = HarnessJson.Read<JsonElement>(request.GetProperty("cells").GetString()!);
        Assert.Equal(177, cells.GetArrayLength());
        var prepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells.EnumerateArray())
        {
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            var before = IdleBattleRunner.DescribeParticipants(await beforeRunner.PrepareAsync(beforeRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await afterRunner.PrepareAsync(afterRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var expected = JsonNode.Parse(before.GetRawText())!;
            var oldGuardian = Assert.Single(expected.AsArray().Where(p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile"))!;
            var newGuardian = Assert.Single(after.EnumerateArray().Where(p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile"));
            foreach (var (name, factor) in new[] { ("Power", .525), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var actual = newGuardian.GetProperty("combatAttributes").GetProperty(name).GetDouble();
                var original = oldGuardian["combatAttributes"]![name]!.GetValue<double>();
                Assert.InRange(Math.Abs(actual - original * factor), 0, .005);
                oldGuardian["combatAttributes"]![name] = actual;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after), "Unexpected prepared participant delta.");
            prepared.Add(cell.GetProperty("id").GetString()!, after);
        }
        HarnessJson.WriteNew(request.GetProperty("output").GetString()!, new { status = "PreparedNoFights", fights = 0, newSeeds = 0, prepared });
    }
}
