using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCeilingPreparationTests
{
    private static JsonNode Floor() => JsonNode.Parse("""
        {"floors":[{"floorNumber":1,"other":"preserve"},{"floorNumber":5,
        "guardianScaling":{"health":3.5366243328,"offense":4.4702934848,"defense":2.65},"stagger":{"maximumBreaks":4}}]}
        """)!;

    [Fact]
    public void Scalar_grid_preserves_every_other_value_without_mutating_or_compounding_baseline()
    {
        var baseline = Floor(); var before = baseline.ToJsonString();
        foreach (var factor in TowerCeilingScreenContract.Factors)
        {
            var result = TowerCeilingScreenContract.ContentVariant(baseline, factor);
            Assert.Equal(before, baseline.ToJsonString());
            var scaling = result["floors"]![1]!["guardianScaling"]!;
            Assert.Equal(TowerCeilingScreenContract.Health * factor, scaling["health"]!.GetValue<decimal>());
            Assert.Equal(TowerCeilingScreenContract.Offense * factor, scaling["offense"]!.GetValue<decimal>());
            scaling["health"] = TowerCeilingScreenContract.Health; scaling["offense"] = TowerCeilingScreenContract.Offense;
            Assert.True(JsonNode.DeepEquals(baseline, result));
        }
        var changed = TowerCeilingScreenContract.ContentVariant(baseline, 1.04m);
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.ContentVariant(changed, 1.04m));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.ContentVariant(baseline, 1.02m));
    }

    private static JsonArray Participants() => new(Enumerable.Range(0, 11).Select(i => (JsonNode)new JsonObject {
        ["slot"] = new JsonObject { ["side"] = i == 10 ? "Hostile" : "Friendly", ["id"] = i },
        ["health"] = 1000, ["combatAttributes"] = new JsonObject { ["Power"] = 100d, ["MaxHealth"] = 1000d, ["Armor"] = 80d },
        ["essences"] = new JsonArray("a", "b", "c") }).ToArray());
    private static JsonElement Element(JsonNode node) => JsonSerializer.SerializeToElement(node);

    [Theory]
    [InlineData("friendly-health")] [InlineData("identity")] [InlineData("essence-order")]
    [InlineData("armor")] [InlineData("missing-character")] [InlineData("incorrect-power")] [InlineData("incorrect-start-health")]
    public void Materialization_rejects_unrelated_or_incorrect_participant_changes(string defect)
    {
        var a = Participants(); var b = (JsonArray)a.DeepClone();
        b[10]!["combatAttributes"]!["Power"] = 104d; b[10]!["combatAttributes"]!["MaxHealth"] = 1040d; b[10]!["health"] = 1040;
        TowerCeilingScreenContract.VerifyParticipants(Element(a), Element(b), 1.04m);
        switch (defect)
        {
            case "friendly-health": b[0]!["health"] = 999; break;
            case "identity": b[0]!["slot"]!["id"] = 22; break;
            case "essence-order": b[0]!["essences"] = new JsonArray("b", "a", "c"); break;
            case "armor": b[10]!["combatAttributes"]!["Armor"] = 81d; break;
            case "missing-character": b.RemoveAt(0); break;
            case "incorrect-power": b[10]!["combatAttributes"]!["Power"] = 105d; break;
            default: b[10]!["health"] = 1039; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.VerifyParticipants(Element(a), Element(b), 1.04m));
    }

    private static IReadOnlyList<IReadOnlyList<int>> Wins(params int[] strongest) => strongest
        .Select(n => (IReadOnlyList<int>)Enumerable.Range(0, 253).Select(i => i == 252 ? n : 0).ToArray()).ToArray();

    [Fact]
    public void Selection_uses_all_four_families_and_prefers_the_frozen_interior_then_smaller_factor()
    {
        Assert.Equal(1.04m, TowerCeilingScreenContract.Select(Wins(100, 38, 38, 33)));
        var borderline = Enumerable.Range(0, 129).First(n => TowerBalanceEvaluator.Wilson(n, 128, 253) is { Upper: <= .5 }
            && TowerCeilingScreenContract.ScreenInterval(n).Upper > .5);
        Assert.False(TowerCeilingScreenContract.Assess(Wins(100, borderline, 100, 100))[1].Eligible);
        Assert.Null(TowerCeilingScreenContract.Select(Wins(38, 0, 100, 100))); // Baseline is never a correction.
    }

    [Fact]
    public void Missing_cells_and_above_ceiling_tail_cannot_be_hidden_by_a_lower_pooled_rate()
    {
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.Assess(Wins(38, 38, 38)));
        var rows = Wins(38, 38, 38, 38).ToArray(); rows[3] = rows[3].SkipLast(1).ToArray();
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.Assess(rows));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenContract.Select(Wins(38, 129, 38, 38)));
        Assert.Null(TowerCeilingScreenContract.Select(Wins(38, 65, 65, 65)));
    }

    [Fact]
    public void Four_sequential_factor_writers_share_one_durable_attempt_and_storage_budget()
    {
        using var temp = new Temp();
        using var journal = new TowerRescreenAttempts(temp.P("attempts"), 4);
        var storage = new TowerStorageAccountant(temp.Path, 20, ["attempts"]);
        for (var i = 0; i < 4; i++)
        {
            var path = temp.P("factor-" + i); storage.BeginDirectory(path); Directory.CreateDirectory(path);
            journal.Record(false); Assert.Equal(i * 2 + 1, new FileInfo(temp.P("attempts")).Length);
            File.WriteAllText(System.IO.Path.Combine(path, "archive"), "abc"); journal.Record(true); storage.SealDirectory();
        }
        Assert.Equal(4, journal.Started); Assert.Equal(4, journal.Completed); Assert.Equal(20, storage.Check()); storage.Audit();
        Assert.Throws<InvalidDataException>(() => journal.Record(false)); journal.Close(); TowerRescreenAttempts.Verify(temp.P("attempts"), 4);
    }

    [Fact]
    public void Interrupted_factor_keeps_its_charge_and_cannot_finish_or_start_the_next_factor()
    {
        using var temp = new Temp(); using var journal = new TowerRescreenAttempts(temp.P("attempts"), 4);
        var storage = new TowerStorageAccountant(temp.Path, 20, ["attempts"]);
        storage.BeginDirectory(temp.P("factor")); Directory.CreateDirectory(temp.P("factor")); journal.Record(false);
        File.WriteAllText(temp.P("factor/.pending"), "partial");
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => storage.Check(stop.Token));
        Assert.Throws<InvalidDataException>(() => storage.Audit());
        Assert.Throws<InvalidDataException>(() => journal.Record(false));
        Assert.Throws<InvalidDataException>(() => storage.BeginDirectory(temp.P("next-factor")));
        journal.Close(); Assert.Equal("S", File.ReadAllText(temp.P("attempts")));
        Assert.Throws<InvalidDataException>(() => TowerRescreenAttempts.Verify(temp.P("attempts"), 1));
        Assert.Throws<IOException>(() => new TowerRescreenAttempts(temp.P("attempts"), 4));
    }

    [Fact]
    public void Saved_preparation_rejects_a_same_length_artifact_edit_and_unlisted_output()
    {
        using var temp = new Temp(); File.WriteAllText(temp.P("prepared"), "abc");
        HarnessJson.WriteNew(temp.P("manifest.json"), new Dictionary<string, string> { ["prepared"] = HarnessJson.FileHash(temp.P("prepared")) });
        TowerBulkCampaign.VerifyFiles(temp.Path, "manifest.json", true, CancellationToken.None);
        File.WriteAllText(temp.P("prepared"), "abd");
        Assert.Throws<InvalidDataException>(() => TowerBulkCampaign.VerifyFiles(temp.Path, "manifest.json", true, CancellationToken.None));
        File.WriteAllText(temp.P("prepared"), "abc"); File.WriteAllText(temp.P("extra"), "");
        Assert.Throws<InvalidDataException>(() => TowerBulkCampaign.VerifyFiles(temp.Path, "manifest.json", true, CancellationToken.None));
    }

    [Fact]
    public async Task Preparation_cancellation_happens_before_output_or_input_access()
    {
        using var temp = new Temp(); using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCeilingScreenPreparation.PrepareAsync("missing", temp.P("out"), stop.Token));
        Assert.False(Directory.Exists(temp.P("out")));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-ceiling-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public string P(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
