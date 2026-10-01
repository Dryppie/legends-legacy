using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class NiRestorationOffenseTests
{
    internal static readonly double[] Factors = [1, 1.05, 1.1];
    private static readonly double[] Offenses = [4.7036132812, 4.9387939453, 5.1739746093];

    internal static void ValidatePlan(JsonElement plan)
    {
        if (!plan.TryGetProperty("offenseFactor", out var factor) || factor.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException("Only the three frozen Ni offense settings are supported.");
        var index = Array.IndexOf(Factors, factor.GetDouble());
        if (index < 0) throw new InvalidDataException("Undeclared offense setting.");
        var expected = NiPenetrationTests.Plan();
        expected["version"] = "tower-ni-restoration-offense-v1";
        expected["offenseFactor"] = Factors[index]; expected["offense"] = Offenses[index];
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" })
        {
            if (!plan.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String ||
                value.GetString() is not { Length: 64 } hash || hash.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
                throw new InvalidDataException("Original catalog pins required.");
            expected[key] = hash;
        }
        if (!JsonElement.DeepEquals(plan, JsonSerializer.SerializeToElement(expected)))
            throw new InvalidDataException("Exact Ni offense diagnostic plan required.");
    }

    internal static void ValidateBatch(JsonElement contract, int batch, JsonElement actualPlan)
    {
        if (batch is < 0 or >= 6) throw new InvalidDataException("Exactly six batches required.");
        var variants = contract.GetProperty("variants");
        if (variants.GetArrayLength() != 3) throw new InvalidDataException("Complete offense grid required.");
        for (var i = 0; i < 3; i++)
        {
            ValidatePlan(variants[i].GetProperty("candidatePlan"));
            if (variants[i].GetProperty("offenseFactor").GetDouble() != Factors[i] ||
                variants[i].GetProperty("candidatePlan").GetProperty("offenseFactor").GetDouble() != Factors[i])
                throw new InvalidDataException("Grid order changed.");
        }
        if (!JsonElement.DeepEquals(actualPlan, variants[batch / 2].GetProperty("candidatePlan")))
            throw new InvalidDataException("Wrong candidate for frozen batch.");
    }

    private static JsonNode Plan(int index)
    {
        var plan = NiPenetrationTests.Plan(); plan["version"] = "tower-ni-restoration-offense-v1";
        plan["offenseFactor"] = Factors[index]; plan["offense"] = Offenses[index];
        foreach (var key in new[] { "sourceTowerSha256", "sourceAbilitiesSha256", "sourceSummonsSha256" }) plan[key] = new string('a', 64);
        return plan;
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Fixed_settings_cannot_be_changed_or_submitted_as_old_acceptance(int index)
    {
        var plan = Plan(index); ValidatePlan(JsonSerializer.SerializeToElement(plan));
        Assert.Throws<InvalidDataException>(() => NiPenetrationTests.ValidatePlan(JsonSerializer.SerializeToElement(plan)));
        foreach (var (key, value) in new[] { ("offenseFactor", .95), ("offense", 4d), ("healthFactor", .9), ("penetrationFactor", 39d), ("penetration", 39d), ("floor", 8d) })
        {
            var changed = plan.DeepClone(); changed[key] = value;
            Assert.Throws<InvalidDataException>(() => ValidatePlan(JsonSerializer.SerializeToElement(changed)));
        }
        foreach (var key in plan.AsObject().Select(p => p.Key).ToArray())
        {
            var changed = plan.DeepClone(); changed.AsObject().Remove(key);
            Assert.Throws<InvalidDataException>(() => ValidatePlan(JsonSerializer.SerializeToElement(changed)));
        }
    }

    [Theory]
    [InlineData("screen", 9, 16, 163, true)]
    [InlineData("confirm", 9, 16, 163, false)]
    [InlineData("search", 9, 16, 163, false)]
    [InlineData("screen", 8, 16, 163, false)]
    [InlineData("screen", 9, 32, 163, false)]
    [InlineData("screen", 9, 16, 148, false)]
    [InlineData("screen", 9, 16, 162, false)]
    [InlineData("screen", 9, 16, 164, false)]
    public void Exact_diagnostic_scope_required(string mode, int floor, int samples, int family, bool allowed)
    {
        void Validate() => BalanceHarnessTowerBalancePassTests.ValidatePanel(mode, floor, samples, BalanceHarnessTowerBalancePassTests.NiRestorationOffenseDiagnostic, family);
        if (allowed) Validate(); else Assert.Throws<InvalidDataException>(Validate);
        Assert.Equal(6, BalanceHarnessTowerBalancePassTests.DiagnosticBatchCount(BalanceHarnessTowerBalancePassTests.NiRestorationOffenseDiagnostic));
    }

    [Fact]
    public void Batch_order_cannot_change_or_extend()
    {
        var contract = JsonSerializer.SerializeToElement(new { variants = Factors.Select((f,i) => new { offenseFactor=f, candidatePlan=Plan(i) }) });
        for (var i=0; i<6; i++)
        {
            ValidateBatch(contract, i, JsonSerializer.SerializeToElement(Plan(i/2)));
            Assert.Throws<InvalidDataException>(() => ValidateBatch(contract, i, JsonSerializer.SerializeToElement(Plan((i/2+1)%3))));
        }
        Assert.Throws<InvalidDataException>(() => ValidateBatch(contract, 6, JsonSerializer.SerializeToElement(Plan(2))));
        Assert.Throws<InvalidDataException>(() => ValidateBatch(contract, -1, JsonSerializer.SerializeToElement(Plan(0))));
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_OFFENSE_PREPARATION")))
                Skip = "Requires the frozen three-setting preparation request; no combat.";
        }
    }

    [PreparationFact]
    public async Task Every_original_and_candidate_preserves_players_and_changes_only_declared_Ni_stats()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_OFFENSE_PREPARATION")!);
        var prior = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", prior.GetProperty("status").GetString());
        var parents = prior.GetProperty("originalPrepared");
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(163, cells.Length); Assert.Equal(parents.EnumerateObject().Select(p=>p.Name),cells.Select(c=>c.GetProperty("id").GetString()));
        var variants = q.GetProperty("variants").EnumerateArray().ToArray(); Assert.Equal(3, variants.Length);
        var source = q.GetProperty("source").GetString()!; var settings = TowerBundle.ReadSettings(source);
        var originalRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source,settings));
        var prepared = new Dictionary<string, Dictionary<string,JsonElement>>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        for (var i=0; i<3; i++)
        {
            var variant = variants[i]; ValidatePlan(variant.GetProperty("candidatePlan"));
            Assert.Equal(Factors[i],variant.GetProperty("candidatePlan").GetProperty("offenseFactor").GetDouble());
            var candidate = variant.GetProperty("candidate").GetString()!;
            var runner = new TowerBattleRunner(candidate,OfflineContent.ForTower(candidate,settings));
            var rows = new Dictionary<string,JsonElement>();
            foreach (var cell in cells)
            {
                var id = cell.GetProperty("id").GetString()!;
                var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds=[0] };
                var before = IdleBattleRunner.DescribeParticipants(await originalRunner.PrepareAsync(originalRunner.CreateInput(scenario,0,settings.Threat,settings.CheckpointIntervalTicks)));
                Assert.True(JsonElement.DeepEquals(parents.GetProperty(id),before),"Original prepared input changed: "+id);
                var after = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(runner.CreateInput(scenario,0,settings.Threat,settings.CheckpointIntervalTicks)));
                var normalized = JsonNode.Parse(before.GetRawText())!.AsArray();
                var ni = Assert.Single(normalized,p=>p!["slot"]!["side"]!.GetValue<string>()=="Hostile")!;
                var actualNi = Assert.Single(after.EnumerateArray(),p=>p.GetProperty("slot").GetProperty("side").GetString()=="Hostile");
                foreach (var (attribute,factor) in new[] { ("Power",Factors[i]),("ArmorPenetration",40d),("MagicPenetration",40d) })
                {
                    var actual=actualNi.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                    Assert.InRange(Math.Abs(actual-ni["combatAttributes"]![attribute]!.GetValue<double>()*factor),0,.02);
                    ni["combatAttributes"]![attribute]=actual;
                }
                Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(normalized),after),"Undeclared prepared difference: "+id);
                rows.Add(id,after);
            }
            prepared.Add(variant.GetProperty("id").GetString()!,rows);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!,new { status="PreparedNoFights",cells=163,nativePreparations=978,fights=0,newSeeds=0,prepared });
    }
}
