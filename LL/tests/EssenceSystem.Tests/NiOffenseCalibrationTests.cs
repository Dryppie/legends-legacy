using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class NiOffenseCalibrationTests
{
    private sealed class PreparationFactAttribute : FactAttribute
    {
        public PreparationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_NI_OFFENSE_PREPARATION")))
                Skip = "Requires the frozen floor-nine offense calibration; prepares without combat.";
        }
    }

    [PreparationFact]
    public async Task Full_family_changes_only_guardian_power_at_each_declared_factor()
    {
        var q = HarnessJson.Read<JsonElement>(Environment.GetEnvironmentVariable("LL_NI_OFFENSE_PREPARATION")!);
        var cells = HarnessJson.Read<JsonElement>(q.GetProperty("cells").GetString()!).EnumerateArray().ToArray();
        Assert.Equal(148, cells.Length);
        var previous = HarnessJson.Read<JsonElement>(q.GetProperty("preparedParents").GetString()!);
        Assert.Equal("PreparedNoFights", previous.GetProperty("status").GetString());
        var source = q.GetProperty("source").GetString()!;
        var settings = TowerBundle.ReadSettings(source);
        var original = new TowerBattleRunner(source, OfflineContent.ForTower(source, settings));
        var variants = q.GetProperty("variants").EnumerateArray().ToArray();
        Assert.Equal(new[] { 1.25, 1.5, 2.0 }, variants.Select(v => v.GetProperty("factor").GetDouble()).ToArray());
        var prepared = new Dictionary<string, Dictionary<string, JsonElement>>();
        using var noFights = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        foreach (var variant in variants)
        {
            var candidate = variant.GetProperty("candidate").GetString()!;
            var factor = variant.GetProperty("factor").GetDouble();
            var runner = new TowerBattleRunner(candidate, OfflineContent.ForTower(candidate, settings));
            var rows = new Dictionary<string, JsonElement>();
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
                var power = actual.GetProperty("combatAttributes").GetProperty("Power").GetDouble();
                Assert.InRange(Math.Abs(power - guardian["combatAttributes"]!["Power"]!.GetValue<double>() * factor), 0, .02);
                guardian["combatAttributes"]!["Power"] = power;
                Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), after), "Difference beyond guardian Power.");
                rows.Add(id, after);
            }
            prepared.Add(variant.GetProperty("id").GetString()!, rows);
        }
        HarnessJson.WriteNew(q.GetProperty("output").GetString()!, new { status = "PreparedNoFights", fights = 0, newSeeds = 0, nativePreparations = 888, prepared });
    }
}
