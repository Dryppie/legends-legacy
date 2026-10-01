using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class NiPenetrationTests
{
    internal static JsonNode Plan() => JsonNode.Parse("""
        {"version":"tower-ni-penetration-v1","floor":9,"healthFactor":1,"offenseFactor":0.95,"penetrationFactor":40,
         "originalOffense":4.7036132812,"offense":4.4684326171,"originalPenetration":1,"penetration":40}
        """)!;

    internal static void ValidatePlan(JsonElement plan)
    {
        var expected = Plan();
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" })
        {
            if (!plan.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String ||
                value.GetString() is not { Length: 64 } hash || hash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
                throw new InvalidDataException("Original catalog SHA-256 pins are required.");
            expected[key] = hash;
        }
        if (!JsonElement.DeepEquals(plan, JsonSerializer.SerializeToElement(expected)))
            throw new InvalidDataException("Only the frozen floor-nine penetration candidate is supported.");
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("overlap")]
    [InlineData("samples")]
    [InlineData("floor")]
    [InlineData("family")]
    [InlineData("version")]
    [InlineData("old-contract")]
    [InlineData("offense")]
    [InlineData("penetration")]
    [InlineData("health")]
    [InlineData("extra")]
    [InlineData("hash")]
    public void Penetration_requires_exact_plan_and_complete_independent_confirmation(string change)
    {
        const string version = "tower-balance-ni-penetration-aggregate-v1";
        var paths = Enumerable.Range(0, 4).Select(i => "batch-" + i).ToArray();
        var plan = Plan();
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" }) plan[key] = new string('a', 64);
        var declaration = JsonSerializer.SerializeToNode(new { version, floor = 9, familySize = 148, samplesPerBatch = 32, batchCount = 4, phases = new { confirm = paths } })!;
        declaration["candidatePlan"] = plan;
        var evidence = JsonSerializer.SerializeToNode(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 148, samples = 128 }, evaluationFights = 18944,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = 4736, seeds = Enumerable.Range(i*32, 32).ToArray() }) })!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(3);
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "samples") declaration["samplesPerBatch"] = 128;
        if (change == "floor") declaration["floor"] = 8;
        if (change == "family") declaration["familySize"] = 147;
        if (change == "version") declaration["version"] = "tower-balance-limited-resistance-aggregate-v1";
        if (change == "old-contract") { declaration["version"] = "tower-balance-aggregate-v1"; evidence["version"] = "tower-balance-aggregate-v1"; }
        if (change == "offense") plan["offenseFactor"] = .9;
        if (change == "penetration") plan["penetrationFactor"] = 39;
        if (change == "health") plan["healthFactor"] = .5;
        if (change == "extra") plan["scalingCoefficient"] = .05;
        if (change == "hash") plan["sourceSummonsSha256"] = "invalid";
        void Validate() => BalanceHarnessTowerBalanceApplicationTests.ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        if (change == "valid") Validate(); else Assert.Throws<InvalidDataException>(Validate);
    }

    [Fact]
    public void Penetration_cannot_fall_back_to_a_gear_label_without_equipment_limits()
    {
        var declaration = JsonSerializer.SerializeToElement(new { candidatePlan = new { version = "tower-ni-penetration-v1" }, gear = "partial" });
        Assert.Throws<InvalidDataException>(() => BalanceHarnessTowerBalanceApplicationTests.ValidateAggregateEquipment(default, declaration, default));
    }

    public static IEnumerable<object[]> TypedDamageCases()
    {
        foreach (var type in new[] { DamageType.Physical, DamageType.Magical })
            foreach (var rating in new[] { 68.22f, 360.54f })
                foreach (var penetration in new[] { .96f, 38.4f, 40f, 60f })
                    yield return [type, rating, penetration];
    }

    [Theory]
    [MemberData(nameof(TypedDamageCases))]
    public void Actual_typed_damage_uses_percentage_point_penetration_and_the_current_cap(DamageType type, float rating, float penetration)
    {
        var hit = AbilityCompiler.CompileAbility(new AbilitySpec { Id = "hit", Name = "Hit", Kind = AbilitySpecKind.Active,
            Effects = [new() { Id = "damage", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.AllEnemies,
                BaseValue = 10000, DamageType = type, CritEligibility = CritEligibility.Disallowed }] });
        var offenseAttribute = type == DamageType.Physical ? AttributeType.ArmorPenetration : AttributeType.MagicPenetration;
        var defenseAttribute = type == DamageType.Physical ? AttributeType.ArmorRating : AttributeType.ResistanceRating;
        var attacker = new RuntimeCombatant("attacker", "attacker", CombatTeam.Friendly,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 100000, [offenseAttribute] = penetration }, [hit], canBasicAttack: false, attributeRulesVersion: AttributeRules.CurrentVersion);
        var target = new RuntimeCombatant("target", "target", CombatTeam.Hostile,
            new Dictionary<AttributeType, float> { [AttributeType.MaxHealth] = 100000, [defenseAttribute] = rating }, [], canBasicAttack: false, attributeRulesVersion: AttributeRules.CurrentVersion);
        var catalog = new JsonAbilityCatalogProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot(), HarnessJson.Options).GetCatalog();
        var engine = new FastCombatEngine(AbilityCompiler.CompileStatuses(catalog.Statuses), AbilityCompiler.CompileSummons(catalog.Summons), AbilityCompiler.CompileAbilities(catalog.Abilities),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 17));
        var result = engine.Run([attacker], [target]);
        var damage = Assert.Single(result.EventLog.Where(e => e.EventType == EventType.Damage && e.TargetId == "target"));
        var mitigation = Math.Max(0, .8 * rating / (rating + 165d) - Math.Min(penetration, 40) / 100d);
        Assert.InRange(Math.Abs(damage.Magnitude - Math.Round(10000 * (1 - mitigation))), 0, 1);
        Assert.InRange(damage.Magnitude, 0, 10000);
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_PENETRATION_PREPARATION")))
                Skip = "Requires the frozen floor-nine penetration trial; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Full_family_changes_only_guardian_power_and_typed_penetration()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_PENETRATION_PREPARATION")!);
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(148, cells.Length);
        var previous = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", previous.GetProperty("status").GetString());
        var source = q.GetProperty("source").GetString()!;
        var candidate = q.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var original = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var runner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var prepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells)
        {
            var id = cell.GetProperty("id").GetString()!;
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            Assert.Equal(9, scenario.FloorNumber);
            var before = IdleBattleRunner.DescribeParticipants(await original.PrepareAsync(original.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(runner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(previous.GetProperty("prepared").GetProperty(id), before), "Original participants changed.");
            var expected = JsonNode.Parse(before.GetRawText())!.AsArray();
            var guardian = Assert.Single(expected.Where(p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile"))!;
            var actual = Assert.Single(after.EnumerateArray().Where(p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile"));
            foreach (var (attribute, factor) in new[] { ("Power", .95), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var value = actual.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(value - guardian["combatAttributes"]![attribute]!.GetValue<double>() * factor), 0, .02);
                if (attribute != "Power") Assert.InRange(Math.Abs(value - 38.4), 0, .001);
                guardian["combatAttributes"]![attribute] = value;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after), "Difference beyond Ni Power and typed penetration.");
            prepared.Add(id, after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new { status = "PreparedNoFights", fights = 0, newSeeds = 0, nativePreparations = 296, prepared });
    }
}
