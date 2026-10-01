using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class TowerOneHealerPreparationTests
{
    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_ONE_HEALER_PREPARATION")))
                Skip = "Requires the pinned one-healer family; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Every_retained_and_one_healer_recipe_matches_exact_native_parent_participants()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_TOWER_ONE_HEALER_PREPARATION")!);
        var proposal = HarnessJson.Read<JsonElement>(q.GetProperty("proposal").GetString()!);
        Assert.Equal("floor8-one-healer-family-proposal-v1", proposal.GetProperty("version").GetString());
        var historical = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", historical.GetProperty("status").GetString());
        var parents = historical.GetProperty("prepared").EnumerateObject().ToDictionary(x => x.Name, x => x.Value);
        Assert.Equal(177, parents.Count);
        var cells = proposal.GetProperty("cells").EnumerateArray().ToArray();
        Assert.Equal(183, cells.Length);
        Assert.Equal(parents.Keys.ToArray(), cells.Take(177).Select(c => c.GetProperty("id").GetString()).ToArray());
        var variants = proposal.GetProperty("variants").EnumerateArray().ToDictionary(v => v.GetProperty("cell").GetProperty("id").GetString()!);
        Assert.Equal(6, variants.Count);
        var expected = parents.ToDictionary(p => p.Key, p => p.Value);
        foreach (var (id, variant) in variants)
        {
            var baseline = JsonNode.Parse(parents[variant.GetProperty("baselineId").GetString()!].GetRawText())!.AsArray();
            var full = parents[variant.GetProperty("sourceId").GetString()!];
            var slot = variant.GetProperty("restorationPartySlot").GetInt32();
            Assert.Contains(slot, new[] { 2, 7 });
            var member = variant.GetProperty("cell").GetProperty("scenario").GetProperty("party").EnumerateArray()
                .Single(m => m.GetProperty("partySlot").GetInt32() == slot);
            var name = member.GetProperty("build").GetProperty("id").GetString();
            var replacement = Assert.Single(full.EnumerateArray().Where(p => p.GetProperty("name").GetString() == name));
            Assert.Equal("Friendly", replacement.GetProperty("slot").GetProperty("side").GetString());
            var indices = Enumerable.Range(0, baseline.Count).Where(i => baseline[i]!["name"]!.GetValue<string>() == name).ToArray();
            var index = Assert.Single(indices);
            Assert.Equal(baseline[index]!["slot"]!["slotId"]!.GetValue<string>(), replacement.GetProperty("slot").GetProperty("slotId").GetString());
            baseline[index] = JsonNode.Parse(replacement.GetRawText());
            expected.Add(id, JsonSerializer.SerializeToElement(baseline));
        }
        var source = q.GetProperty("source").GetString()!;
        var candidate = q.GetProperty("candidate").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var originalRunner = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var candidateRunner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
        var originalPrepared = new Dictionary<string, JsonElement>();
        var candidatePrepared = new Dictionary<string, JsonElement>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var cell in cells)
        {
            var id = cell.GetProperty("id").GetString()!;
            var scenario = cell.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
            Assert.Equal(8, scenario.FloorNumber);
            var before = IdleBattleRunner.DescribeParticipants(await originalRunner.PrepareAsync(originalRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await candidateRunner.PrepareAsync(candidateRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            Assert.True(JsonElement.DeepEquals(expected[id], after), "One-healer preparation differs from exact saved parents: " + id);
            var normalized = JsonNode.Parse(before.GetRawText())!.AsArray();
            var guardian = Assert.Single(normalized.Where(p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile"))!;
            var changed = Assert.Single(after.EnumerateArray().Where(p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile"));
            foreach (var (attribute, factor) in new[] { ("Power", .525), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var actual = changed.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(actual - guardian["combatAttributes"]![attribute]!.GetValue<double>() * factor), 0, .005);
                guardian["combatAttributes"]![attribute] = actual;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(normalized), after), "Undeclared catalog preparation difference: " + id);
            originalPrepared.Add(id, before); candidatePrepared.Add(id, after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new {
            status = "PreparedNoFights", cells = 183, nativePreparations = 366, fights = 0, newSeeds = 0,
            originalPrepared, candidatePrepared });
    }
}
