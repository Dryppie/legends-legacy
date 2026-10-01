using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class NiCopyHealthTests
{
    internal static JsonNode Plan() => JsonNode.Parse("""
        {"version":"tower-ni-copy-health-v1","floor":9,"summonId":"niCopy","attribute":"MaxHealth",
         "originalScalingCoefficient":0.1,"scalingCoefficient":0.05,"abilityId":"ability.creature.ni.ninefold",
         "originalDescription":"Innate: summon 9 inert copies with 10% of Ni's Max Health and inherited Armor and Resistance. Whenever a copy dies, Ni permanently gains 5% of initial Power.",
         "description":"Innate: summon 9 inert copies with 5% of Ni's Max Health and inherited Armor and Resistance. Whenever a copy dies, Ni permanently gains 5% of initial Power."}
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
            throw new InvalidDataException("Only the frozen floor-nine copy-Health candidate is supported.");
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
    [InlineData("coefficient")]
    [InlineData("extra")]
    [InlineData("hash")]
    [InlineData("description")]
    public void Copy_health_requires_exact_plan_and_complete_independent_confirmation(string change)
    {
        const string version = "tower-balance-ni-copy-health-aggregate-v1";
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
        if (change == "coefficient") plan["scalingCoefficient"] = .04;
        if (change == "extra") plan["offenseFactor"] = .5;
        if (change == "hash") plan["sourceSummonsSha256"] = "invalid";
        if (change == "description") plan["description"] = "changed";
        void Validate() => BalanceHarnessTowerBalanceApplicationTests.ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        if (change == "valid") Validate(); else Assert.Throws<InvalidDataException>(Validate);
    }

    [Theory]
    [InlineData(DamageType.Physical)]
    [InlineData(DamageType.Magical)]
    public void Candidate_copies_have_five_percent_health_unchanged_defense_behavior_and_no_actions(DamageType type)
    {
        var hit = AbilityCompiler.CompileAbility(new AbilitySpec { Id = "hit", Name = "Hit", Kind = AbilitySpecKind.Active,
            Effects = [new() { Id = "damage", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.AllEnemies,
                BaseValue = 100, DamageType = type, CritEligibility = CritEligibility.Disallowed }] });
        // Compare the existing engine behavior rather than assuming authored Armor
        // is still a percentage under current rules. The separate Health candidate
        // must not silently repair the existing summon defense-unit conversion.
        var originalCatalog = Catalog();
        var original = Engine(originalCatalog).Run([Actor("ni", CombatTeam.Friendly,
            [AbilityCompiler.CompileAbility(originalCatalog.AbilitiesById["ability.creature.ni.ninefold"])],
            new() { [AttributeType.Armor] = 40, [AttributeType.Resistance] = 30 })],
            [Actor("enemy", CombatTeam.Hostile, [hit], new())]);
        var catalog = Catalog();
        catalog.SummonsById["niCopy"].Attributes.Single(a => a.Attribute == AttributeType.MaxHealth).ScalingCoefficient = .05f;
        var result = Engine(catalog).Run([Actor("ni", CombatTeam.Friendly,
            [AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninefold"])],
            new() { [AttributeType.Armor] = 40, [AttributeType.Resistance] = 30 })],
            [Actor("enemy", CombatTeam.Hostile, [hit], new())]);
        var copies = result.EntityStats.Where(e => e.EntityName == "Ninefold Copy").ToArray();
        Assert.Equal(9, copies.Length);
        Assert.All(copies, copy =>
        {
            Assert.Equal(500, copy.MaxHealth);
            var damage = Assert.Single(result.EventLog.Where(e => e.TargetId == copy.EntityId && e.Source == "damage"));
            var baseline = Assert.Single(original.EventLog.Where(e => e.TargetId == copy.EntityId && e.Source == "damage"));
            Assert.Equal(baseline.Magnitude, damage.Magnitude);
            Assert.Equal(baseline.IncomingRawDamage, damage.IncomingRawDamage);
            Assert.DoesNotContain(result.EventLog, e => e.ActorId == copy.EntityId && e.EventType is EventType.Damage or EventType.AbilityUse);
        });
    }

    [Fact]
    public void Candidate_copy_deaths_still_grant_permanent_initial_power()
    {
        var catalog = Catalog();
        catalog.SummonsById["niCopy"].Attributes.Single(a => a.Attribute == AttributeType.MaxHealth).ScalingCoefficient = .05f;
        var ni = Actor("ni", CombatTeam.Friendly, [AbilityCompiler.CompileAbility(catalog.AbilitiesById["ability.creature.ni.ninefold"])], new());
        var hit = AbilityCompiler.CompileAbility(new AbilitySpec { Id = "sweep", Name = "Sweep", Kind = AbilitySpecKind.Active,
            Effects = [new() { Id = "damage", Operation = AbilityEffectOperation.Damage, Target = AbilityTargetSelector.AllEnemies,
                BaseValue = 501, DamageType = DamageType.None, CritEligibility = CritEligibility.Disallowed }] });
        var result = Engine(catalog).Run([ni], [Actor("enemy", CombatTeam.Hostile, [hit], new())]);
        Assert.Equal(145, ni.GetAttribute(AttributeType.Power));
        Assert.Equal(9, result.EventLog.Count(e => e.Source == "effect.creature.ni.ninefold.power" && e.EventType == EventType.Buff));
    }

    private static AbilityCatalog Catalog() => new JsonAbilityCatalogProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot(), HarnessJson.Options).GetCatalog();
    private static FastCombatEngine Engine(AbilityCatalog catalog) => new(AbilityCompiler.CompileStatuses(catalog.Statuses), AbilityCompiler.CompileSummons(catalog.Summons), AbilityCompiler.CompileAbilities(catalog.Abilities), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 17));
    private static RuntimeCombatant Actor(string id, CombatTeam team, IReadOnlyList<CompiledAbility> abilities, Dictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = 10000; attributes[AttributeType.Power] = 100;
        return new RuntimeCombatant(id, id, team, attributes, abilities, canBasicAttack: false, attributeRulesVersion: AttributeRules.CurrentVersion);
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_COPY_HEALTH_PREPARATION")))
                Skip = "Requires the explicit archived family and isolated candidate; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Complete_floor_nine_family_preserves_original_prepared_participants()
    {
        var request = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_COPY_HEALTH_PREPARATION")!);
        var source = request.GetProperty("source").GetString()!; var candidate = request.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var beforeRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var afterRunner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var cells = HarnessJson.Read<JsonElement>(request.GetProperty("cells").GetString()!);
        Assert.Equal(148, cells.GetArrayLength());
        var prepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells.EnumerateArray())
        {
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            var before = IdleBattleRunner.DescribeParticipants(await beforeRunner.PrepareAsync(beforeRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await afterRunner.PrepareAsync(afterRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(before, after), "Copy Health must not alter initial participants.");
            prepared.Add(cell.GetProperty("id").GetString()!, after);
        }
        HarnessJson.WriteNew(request.GetProperty("output").GetString()!, new { status = "PreparedNoFights", fights = 0, newSeeds = 0, prepared });
    }
}
