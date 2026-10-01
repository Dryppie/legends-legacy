using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class NiRestorationAcceptanceTests
{
    internal static JsonNode Plan() => JsonNode.Parse("""
        {"version":"tower-ni-restoration-acceptance-v1","floor":9,"healthFactor":1,"offenseFactor":1.0,"penetrationFactor":40,
         "originalOffense":4.7036132812,"offense":4.7036132812,"originalPenetration":1,"penetration":40}
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
    public void Restoration_requires_exact_plan_and_eight_complete_independent_batches(string change)
    {
        const string version = "tower-balance-ni-restoration-aggregate-v1";
        var paths = Enumerable.Range(0, 8).Select(i => "batch-" + i).ToArray();
        var plan = Plan();
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" }) plan[key] = new string('a', 64);
        var declaration = JsonSerializer.SerializeToNode(new { version, floor = 9, familySize = 163, samplesPerBatch = 64, batchCount = 8, phases = new { confirm = paths } })!;
        declaration["candidatePlan"] = plan;
        var evidence = JsonSerializer.SerializeToNode(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 163, samples = 512 }, evaluationFights = 83456,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = 10432, seeds = Enumerable.Range(i*64, 64).ToArray() }) })!;
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

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_ACCEPTANCE_PREPARATION")))
                Skip = "Requires the frozen nominated candidate; no combat.";
        }
    }

    [PreparationFact]
    public async Task Every_candidate_matches_nominated_native_inputs_with_unchanged_original_offense()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_ACCEPTANCE_PREPARATION")!);
        var proposal = HarnessJson.Read<JsonElement>(q.GetProperty("proposal").GetString()!); ValidatePlan(proposal.GetProperty("candidatePlan"));
        var prior = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", prior.GetProperty("status").GetString());
        var parents = prior.GetProperty("prepared").GetProperty("offense-1.00");
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(163, cells.Length); Assert.Equal(parents.EnumerateObject().Select(p=>p.Name),cells.Select(c=>c.GetProperty("id").GetString()));
        var source=q.GetProperty("source").GetString()!;var candidate=q.GetProperty("candidate").GetString()!;var settings=TowerBundle.ReadSettings(source);
        var beforeRunner=new TowerBattleRunner(source,OfflineContent.ForTower(source,settings));
        var afterRunner=new TowerBattleRunner(candidate,OfflineContent.ForTower(candidate,settings));
        var prepared=new Dictionary<string,JsonElement>();
        using var noFights=new TowerPerformanceTrace(_=>throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells)
        {
            var id=cell.GetProperty("id").GetString()!;
            var scenario=cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds=[0] };
            var before=IdleBattleRunner.DescribeParticipants(await beforeRunner.PrepareAsync(beforeRunner.CreateInput(scenario,0,settings.Threat,settings.CheckpointIntervalTicks)));
            var after=IdleBattleRunner.DescribeParticipants(await afterRunner.PrepareAsync(afterRunner.CreateInput(scenario,0,settings.Threat,settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(parents.GetProperty(id),after),"Nominated prepared input changed: "+id);
            var normalized=JsonNode.Parse(before.GetRawText())!.AsArray();
            var ni=Assert.Single(normalized,p=>p!["slot"]!["side"]!.GetValue<string>()=="Hostile")!;
            var actualNi=Assert.Single(after.EnumerateArray(),p=>p.GetProperty("slot").GetProperty("side").GetString()=="Hostile");
            foreach (var attribute in new[] { "ArmorPenetration","MagicPenetration" })
            {
                var actual=actualNi.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(actual-ni["combatAttributes"]![attribute]!.GetValue<double>()*40),0,.02);
                ni["combatAttributes"]![attribute]=actual;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(normalized),after),"Undeclared participant difference: "+id);
            prepared.Add(id,after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!,new { status="PreparedNoFights",cells=163,nativePreparations=326,fights=0,newSeeds=0,prepared });
    }
}
