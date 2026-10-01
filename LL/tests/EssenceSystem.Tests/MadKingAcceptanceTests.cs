using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class MadKingAcceptanceTests
{
    internal static JsonNode Plan() => JsonNode.Parse("""
        {"version":"tower-mad-king-penetration-acceptance-v1","floor":10,"healthFactor":1,"offenseFactor":0.5,"penetrationFactor":40,
         "originalOffense":7.13,"offense":3.565,"originalPenetration":1,"penetration":40}
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
            throw new InvalidDataException("Only the nominated floor-ten Power/penetration setting is supported.");
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("overlap")]
    [InlineData("reordered")]
    [InlineData("samples")]
    [InlineData("batches")]
    [InlineData("floor")]
    [InlineData("family")]
    [InlineData("version")]
    [InlineData("old-contract")]
    [InlineData("offense")]
    [InlineData("penetration")]
    [InlineData("health")]
    [InlineData("extra")]
    [InlineData("hash")]
    [InlineData("diagnostic")]
    public void Confirmation_requires_every_fixed_batch_and_exact_candidate(string change)
    {
        const string version = "tower-balance-mad-king-acceptance-aggregate-v1";
        var paths = Enumerable.Range(0, 32).Select(i => "batch-" + i).ToArray();
        var plan = Plan();
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" }) plan[key] = new string('a', 64);
        var declaration = JsonSerializer.SerializeToNode(new { version, floor = 10, familySize = 278, samplesPerBatch = 16, batchCount = 32, phases = new { confirm = paths } })!;
        declaration["candidatePlan"] = plan;
        var evidence = JsonSerializer.SerializeToNode(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 278, samples = 512 }, evaluationFights = 142336,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = 4448, seeds = Enumerable.Range(i*16, 16).ToArray() }) })!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(31);
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "reordered") evidence["batches"]![1]!["source"] = "batch-0";
        if (change == "samples") declaration["samplesPerBatch"] = 32;
        if (change == "batches") declaration["batchCount"] = 16;
        if (change == "floor") declaration["floor"] = 9;
        if (change == "family") declaration["familySize"] = 277;
        if (change == "version") declaration["version"] = "tower-balance-aggregate-v1";
        if (change == "old-contract") { declaration["version"] = "tower-balance-aggregate-v1"; evidence["version"] = "tower-balance-aggregate-v1"; }
        if (change == "offense") plan["offenseFactor"] = .55;
        if (change == "penetration") plan["penetrationFactor"] = 39;
        if (change == "health") plan["healthFactor"] = .9;
        if (change == "extra") plan["extra"] = true;
        if (change == "hash") plan["sourceSummonsSha256"] = "invalid";
        if (change == "diagnostic") plan["version"] = "floor10-penetration-diagnostic-v1";
        void Validate() => BalanceHarnessTowerBalanceApplicationTests.ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        if (change == "valid") Validate(); else Assert.Throws<InvalidDataException>(Validate);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    public void Equipment_contract_cannot_be_weakened(string change)
    {
        var declaration = JsonSerializer.SerializeToNode(new { candidatePlan = new { version = "tower-mad-king-penetration-acceptance-v1" },
            equipmentEligibility = new { version = "tower-limited-equipment-v1", maximumSpecializedItems = 8, maximumSpecializedCharacters = 2 } })!;
        if (change == "missing") declaration.AsObject().Remove("equipmentEligibility");
        if (change == "items") declaration["equipmentEligibility"]!["maximumSpecializedItems"] = 9;
        if (change == "characters") declaration["equipmentEligibility"]!["maximumSpecializedCharacters"] = 3;
        if (change == "gear") declaration["gear"] = "baseline";
        Assert.Throws<InvalidDataException>(() => BalanceHarnessTowerBalanceApplicationTests.ValidateAggregateEquipment(default, JsonSerializer.SerializeToElement(declaration), default));
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_MAD_KING_ACCEPTANCE_PREPARATION")))
                Skip = "Requires the fixed nominated candidate; no combat.";
        }
    }

    [PreparationFact]
    public async Task Every_candidate_preserves_players_and_matches_the_nominated_native_guardian()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_MAD_KING_ACCEPTANCE_PREPARATION")!);
        ValidatePlan(HarnessJson.Read<JsonElement>(q.GetProperty("plan").GetString()!));
        var parents = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(278, cells.Length);
        Assert.Equal(parents.EnumerateObject().Select(p => p.Name), cells.Select(c => c.GetProperty("id").GetString()));
        var source = q.GetProperty("source").GetString()!; var candidate = q.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var beforeRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var afterRunner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var prepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells)
        {
            var id = cell.GetProperty("id").GetString()!;
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            var before = IdleBattleRunner.DescribeParticipants(await beforeRunner.PrepareAsync(beforeRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await afterRunner.PrepareAsync(afterRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(parents.GetProperty(id), after), "Nominated preparation changed: " + id);
            var normalized = JsonNode.Parse(before.GetRawText())!.AsArray();
            var guardian = Assert.Single(normalized, p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
            var actual = Assert.Single(after.EnumerateArray(), p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile");
            foreach (var (attribute, factor) in new[] { ("Power", .5), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var value = actual.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(value - guardian["combatAttributes"]![attribute]!.GetValue<double>()*factor), 0, .002);
                guardian["combatAttributes"]![attribute] = value;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(normalized), after), "Undeclared participant difference: " + id);
            prepared.Add(id, after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new { status = "PreparedNoFights", cells = 278, nativePreparations = 556, fights = 0, newSeeds = 0, prepared });
    }
}
