using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class NiLimitedRestorationTests
{
    [Theory]
    [InlineData("screen", 9, 16, 163, true)]
    [InlineData("confirm", 9, 16, 163, false)]
    [InlineData("search", 9, 16, 163, false)]
    [InlineData("prepare", 9, 16, 163, false)]
    [InlineData("screen", 8, 16, 163, false)]
    [InlineData("screen", 9, 8, 163, false)]
    [InlineData("screen", 9, 32, 163, false)]
    [InlineData("screen", 9, 16, 148, false)]
    [InlineData("screen", 9, 16, 162, false)]
    [InlineData("screen", 9, 16, 164, false)]
    public void Restoration_diagnostic_requires_exact_non_acceptance_panel(string mode, int floor, int samples, int family, bool allowed)
    {
        void Validate() => BalanceHarnessTowerBalancePassTests.ValidatePanel(mode, floor, samples, BalanceHarnessTowerBalancePassTests.NiRestorationDiagnostic, family);
        if (allowed) Validate(); else Assert.Throws<InvalidDataException>(Validate);
        Assert.Equal(2, BalanceHarnessTowerBalancePassTests.DiagnosticBatchCount(BalanceHarnessTowerBalancePassTests.NiRestorationDiagnostic));
    }

    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_PREPARATION")))
                Skip = "Requires the frozen limited-Restoration proposal; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Full_family_preserves_controls_and_changes_only_declared_healer_equipment()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_RESTORATION_PREPARATION")!);
        var proposal = HarnessJson.Read<JsonElement>(q.GetProperty("proposal").GetString()!);
        Assert.Equal("floor9-limited-restoration-proposal-v1", proposal.GetProperty("version").GetString());
        NiPenetrationTests.ValidatePlan(proposal.GetProperty("candidatePlan"));
        var historical = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", historical.GetProperty("status").GetString());
        var old = historical.GetProperty("prepared");
        var cells = proposal.GetProperty("cells").EnumerateArray().ToArray();
        Assert.Equal(163, cells.Length);
        Assert.Equal(old.EnumerateObject().Select(p => p.Name), cells.Take(148).Select(c => c.GetProperty("id").GetString()));
        var lookup = cells.ToDictionary(c => c.GetProperty("id").GetString()!);
        var variants = proposal.GetProperty("variants").EnumerateArray().ToDictionary(v => v.GetProperty("cell").GetProperty("id").GetString()!);
        Assert.Equal(15, variants.Count);
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
            Assert.Equal(9, scenario.FloorNumber);
            var before = IdleBattleRunner.DescribeParticipants(await originalRunner.PrepareAsync(originalRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            var after = IdleBattleRunner.DescribeParticipants(await candidateRunner.PrepareAsync(candidateRunner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks)));
            if (variants.TryGetValue(id, out var variant))
            {
                var baselineId = variant.GetProperty("baselineId").GetString()!;
                var expected = JsonNode.Parse(lookup[baselineId].GetProperty("scenario").GetRawText())!;
                var parent = lookup[variant.GetProperty("sourceId").GetString()!].GetProperty("scenario");
                var selected = variant.GetProperty("specializedPartySlots").EnumerateArray().Select(p => p.GetInt32()).ToArray();
                Assert.True(selected.SequenceEqual([2]) || selected.SequenceEqual([7]) || selected.SequenceEqual([2,7]));
                var slots = selected.Length == 2 ? new[] { "MainHand", "Chest", "Head", "Necklace" } : ["MainHand", "Chest", "Head", "Ring", "Necklace", "Relic"];
                Assert.Equal(selected.Length == 2 ? 8 : 6, variant.GetProperty("specializedItems").GetInt32());
                foreach (var member in expected["party"]!.AsArray())
                {
                    var partySlot = member!["partySlot"]!.GetValue<int>(); if (!selected.Contains(partySlot)) continue;
                    var donor = parent.GetProperty("party").EnumerateArray().Single(p => p.GetProperty("partySlot").GetInt32() == partySlot);
                    var equipment = member["build"]!["equipment"]!.AsArray();
                    for (var i = 0; i < equipment.Count; i++)
                    {
                        var slot = equipment[i]!["slot"]!.GetValue<string>();
                        if (slots.Contains(slot)) equipment[i] = JsonNode.Parse(donor.GetProperty("build").GetProperty("equipment").EnumerateArray().Single(e => e.GetProperty("slot").GetString() == slot).GetRawText());
                    }
                }
                Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), cell.GetProperty("scenario")), "Exact gear derivation changed: " + id);
                var derived = expected.Deserialize<TowerScenario>(HarnessJson.Options)! with { Seeds = [0] };
                var independentlyPrepared = IdleBattleRunner.DescribeParticipants(await candidateRunner.PrepareAsync(candidateRunner.CreateInput(derived, 0, settings.Threat, settings.CheckpointIntervalTicks)));
                Assert.True(JsonElement.DeepEquals(independentlyPrepared, after));
                var baseline = old.GetProperty(baselineId).EnumerateArray().ToArray();
                var selectedNames = derived.Party.Where(p => selected.Contains(p.PartySlot)).Select(p => p.Build.Id).ToHashSet();
                foreach (var (original, actual) in baseline.Zip(after.EnumerateArray()))
                    if (!selectedNames.Contains(original.GetProperty("name").GetString()!)) Assert.True(JsonElement.DeepEquals(original, actual), "Unchosen participant changed.");
            }
            else Assert.True(JsonElement.DeepEquals(old.GetProperty(id), after), "Retained participant changed: " + id);
            var normalized = JsonNode.Parse(before.GetRawText())!.AsArray();
            var guardian = Assert.Single(normalized, p => p!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
            var changed = Assert.Single(after.EnumerateArray(), p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile");
            foreach (var (attribute, factor) in new[] { ("Power", .95), ("ArmorPenetration", 40d), ("MagicPenetration", 40d) })
            {
                var actual = changed.GetProperty("combatAttributes").GetProperty(attribute).GetDouble();
                Assert.InRange(Math.Abs(actual - guardian["combatAttributes"]![attribute]!.GetValue<double>() * factor), 0, .02);
                guardian["combatAttributes"]![attribute] = actual;
            }
            Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(normalized), after), "Undeclared catalog difference: " + id);
            originalPrepared.Add(id, before); candidatePrepared.Add(id, after);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new { status = "PreparedNoFights", cells = 163, nativePreparations = 341,
            fights = 0, newSeeds = 0, originalPrepared, candidatePrepared });
    }
}
