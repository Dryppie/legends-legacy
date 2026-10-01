using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class KodokuRefinementDiagnosticTests
{
    [Theory]
    [InlineData("screen", 8, 16, 186, true)]
    [InlineData("confirm", 8, 16, 186, false)]
    [InlineData("search", 8, 16, 186, false)]
    [InlineData("screen", 7, 16, 186, false)]
    [InlineData("screen", 8, 32, 186, false)]
    [InlineData("screen", 8, 16, 185, false)]
    public void Offense_diagnostic_cannot_change_mode_floor_panel_or_family(string mode, int floor, int samples, int family, bool allowed)
    {
        if (allowed) BalanceHarnessTowerBalancePassTests.ValidatePanel(mode, floor, samples, BalanceHarnessTowerBalancePassTests.KodokuRefinementDiagnostic, family);
        else Assert.Throws<InvalidDataException>(() => BalanceHarnessTowerBalancePassTests.ValidatePanel(mode, floor, samples, BalanceHarnessTowerBalancePassTests.KodokuRefinementDiagnostic, family));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Initial_and_insect_jar_summons_scale_power_at_point_five_seven_five_without_changing_health_or_penetration(bool initial)
    {
        var catalog = new JsonAbilityCatalogProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            TestContentPaths.FindApiRoot(), HarnessJson.Options).GetCatalog();
        KodokuSharedPenetrationTests.EnsureCandidatePenetration(catalog);
        var ability = catalog.AbilitiesById[initial ? "ability.creature.kodoku.survivors_struggle" : "ability.creature.kodoku.insect_jar"];
        var owner = new RuntimeCombatant("owner", "owner", CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000, [AttributeType.Power] = 1000 * .575f, [AttributeType.ArmorPenetration] = 40.32f },
            [AbilityCompiler.CompileAbility(ability)], canBasicAttack: false, attributeRulesVersion: AttributeRules.CurrentVersion);
        var target = new RuntimeCombatant("target", "target", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 10000, [AttributeType.Armor] = 60 }, [], canBasicAttack: false,
            attributeRulesVersion: AttributeRules.CurrentVersion);
        var result = new FastCombatEngine(AbilityCompiler.CompileStatuses(catalog.Statuses), AbilityCompiler.CompileSummons(catalog.Summons),
            AbilityCompiler.CompileAbilities(catalog.Abilities), new FastCombatEngineOptions(MaxTicks: 2, BasicAttackIntervalTicks: 1, RandomSeed: 17)).Run([owner], [target]);
        var spawns = result.EntityStats.Where(x => x.EntityName == "Venomspawn").ToArray();
        Assert.Equal(initial ? 5 : 3, spawns.Length);
        Assert.All(spawns, spawn => {
            Assert.Equal(800, spawn.MaxHealth);
            var hits = result.EventLog.Where(e => e.ActorId == spawn.EntityId && e.EventType == EventType.Damage).ToArray();
            Assert.NotEmpty(hits);
            Assert.All(hits, hit => {
                // 15% inherited Power = 86.25; basic damage = 1 + 86.25/2, then the ±20% roll.
                Assert.InRange(hit.IncomingRawDamage, 35, 53);
                Assert.Equal((int)Math.Round(hit.IncomingRawDamage * .8), hit.Magnitude);
            });
        });
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_KODOKU_REFINEMENT_PREPARATION")))
                Skip = "Requires the frozen 186-recipe offense proposal; no combat.";
        }
    }

    [PreparationFact]
    public async Task All_186_recipes_preserve_every_participant_except_declared_guardian_scaling()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_KODOKU_REFINEMENT_PREPARATION")!);
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(186, cells.Length);
        var previous = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", previous.GetProperty("status").GetString());
        var source = q.GetProperty("source").GetString()!; var candidate = q.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var originalRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var candidateRunner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var originalPrepared = new Dictionary<string, JsonElement>(); var candidatePrepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells)
        {
            var id = cell.GetProperty("id").GetString()!;
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            var before = IdleBattleRunner.DescribeParticipants(await originalRunner.PrepareAsync(originalRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await candidateRunner.PrepareAsync(candidateRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(previous.GetProperty("originalPrepared").GetProperty(id), before), "Original native participants changed.");
            var expected = JsonNode.Parse(before.GetRawText())!.AsArray();
            var guardian = Assert.Single(expected.Where(p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile"))!;
            var changed = Assert.Single(after.EnumerateArray().Where(p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile"));
            foreach (var (attribute, factor) in new[] { ("Power", .575), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var actual = changed.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(actual - guardian["combatAttributes"]![attribute]!.GetValue<double>() * factor), 0, .005);
                guardian["combatAttributes"]![attribute] = actual;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after), "Undeclared original/candidate difference.");
            var earlier = JsonNode.Parse(previous.GetProperty("candidatePrepared").GetProperty(id).GetRawText())!.AsArray();
            var oldGuardian = Assert.Single(earlier.Where(p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile"))!;
            var power = changed.GetProperty("combatAttributes").GetProperty("Power").GetDouble();
            Assert.InRange(Math.Abs(power - oldGuardian["combatAttributes"]!["Power"]!.GetValue<double>() * .575 / .6), 0, .02);
            oldGuardian["combatAttributes"]!["Power"] = power;
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(earlier), after), "Difference beyond guardian Power from prior candidate.");
            originalPrepared.Add(id, before); candidatePrepared.Add(id, after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new { status = "PreparedNoFights", cells = 186,
            nativePreparations = 372, fights = 0, newSeeds = 0, originalPrepared, candidatePrepared });
    }
}
