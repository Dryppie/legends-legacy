using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerRetainedAuditTests
{
    private static TowerSearchBudget Budget => TowerPartyProgression.Budget(5) with { PriorityFloor = 5 };
    private static TowerScenario Scenario
    {
        get
        {
            var original = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
            return original with { FloorNumber = 5, Seeds = [], Party = Enumerable.Range(1, 10).Select(i =>
                new TowerPartyRecipe(i, original.Party[(i - 1) % 5].Build with { Id = $"synthetic-{i}",
                    CharacterLevel = Budget.CharacterLevel, Tier = Budget.Tier, Rank = Budget.Rank, Quality = Budget.Quality,
                    EssenceIds = original.Party[(i - 1) % 5].Build.EssenceIds.Take(4).Append("synthetic-fifth").ToArray(),
                    IdentityEssenceIds = null })).ToArray() };
        }
    }
    private static TowerBalanceCohort Cohort => new("cohort", Budget,
        10, "fixed", TowerBossDiscovery.EquipmentBudgetHash(Scenario.Party));
    private static JsonElement Participants(int health = 100) => JsonSerializer.SerializeToElement(Enumerable.Range(0, 11)
        .Select(i => new { slot = new { side = i < 10 ? "Friendly" : "Hostile", index = i }, health }).ToArray());
    private static object Row(int i, TowerScenario? scenario = null) => new {
        key = i.ToString("x64"), scenario = scenario ?? Scenario, sources = new[] { new { source = "fixture", row = i } } };

    [Fact]
    public void Native_identity_preserves_context_characters_and_essence_order()
    {
        var s = Scenario; var original = TowerRetainedFamilyAudit.Identity(s);
        Assert.NotEqual(original.CellHash, TowerRetainedFamilyAudit.Identity(s with { Id = "different-context" }).CellHash);
        Assert.NotEqual(original.CellHash, TowerRetainedFamilyAudit.Identity(s with { StartsAt = s.StartsAt.AddSeconds(1) }).CellHash);
        var changed = s.Party.Select((p, i) => i == 0 ? p with { Build = p.Build with { Id = "different-character" } } : p).ToArray();
        Assert.NotEqual(original.RecipeHash, TowerRetainedFamilyAudit.Identity(s with { Party = changed }).RecipeHash);
        changed = s.Party.Select((p, i) => i == 0 ? p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } } : p).ToArray();
        Assert.NotEqual(original.RecipeHash, TowerRetainedFamilyAudit.Identity(s with { Party = changed }).RecipeHash);
    }

    [Fact]
    public void Only_production_equivalent_order_and_identity_defaults_share_a_recipe()
    {
        var s = Scenario; var reordered = s with { Party = s.Party.Reverse().Select(p => p with { Build = p.Build with {
            Equipment = p.Build.Equipment.Reverse().ToArray(), IdentityEssenceIds = p.Build.IdentityEssenceIds ?? p.Build.EssenceIds } }).ToArray() };
        Assert.Equal(TowerRetainedFamilyAudit.Identity(s), TowerRetainedFamilyAudit.Identity(reordered));
        var parsed = TowerRetainedFamilyAudit.Scenario(JsonSerializer.SerializeToElement(reordered, HarnessJson.Options), Cohort, s);
        Assert.Equal(Enumerable.Range(1, 10), parsed.Party.Select(p => p.PartySlot));
    }

    [Theory]
    [InlineData("unknown")] [InlineData("seeds")] [InlineData("floor")] [InlineData("time")]
    [InlineData("preparation")] [InlineData("level")] [InlineData("styles")] [InlineData("missing-party")]
    public void Incompatible_context_and_unknown_fields_are_not_silently_normalized(string defect)
    {
        var s = JsonSerializer.SerializeToNode(Scenario, HarnessJson.Options)!;
        if (defect == "unknown") s["futureMechanic"] = true;
        if (defect == "seeds") s["seeds"] = new JsonArray(123);
        if (defect == "floor") s["floorNumber"] = 6;
        if (defect == "time") s["startsAt"] = Scenario.StartsAt.AddSeconds(1).ToString("O");
        if (defect == "preparation") s["preparationState"] = "contributions";
        if (defect == "level") s["party"]![0]!["build"]!["characterLevel"] = 99;
        if (defect == "styles") s["party"]![0]!["build"]!["equipment"]![0]!["useNativeStyle"] = true;
        if (defect == "missing-party") s.AsObject().Remove("party");
        Assert.ThrowsAny<Exception>(() => TowerRetainedFamilyAudit.Scenario(JsonSerializer.SerializeToElement(s), Cohort, Scenario));
    }

    private static async Task<(TowerRetainedAuditResult Result, string Output)> Scan(object[] rows, int expected = 2,
        Func<TowerScenario, CancellationToken, Task<(string, JsonElement)>>? prepare = null,
        IReadOnlyDictionary<string, (string, string)>? required = null, Action? check = null, CancellationToken ct = default)
    {
        using var input = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(rows, HarnessJson.Options)); using var output = new MemoryStream();
        var result = await TowerRetainedFamilyAudit.Scan(input, output, expected, Cohort, Scenario, required ?? new Dictionary<string, (string, string)>(),
            prepare ?? ((_, _) => Task.FromResult((new string('b', 64), Participants()))), check ?? (() => { }), ct);
        return (result, System.Text.Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task Complete_stream_retains_all_entries_and_maps_equivalent_aliases_and_midpoint()
    {
        var s = Scenario; var required = new Dictionary<string, (string, string)> {
            [TowerRetainedFamilyAudit.Identity(s).CellHash] = ("required", HarnessJson.Hash(Participants())) };
        var (r, output) = await Scan([Row(1), Row(2)], required: required);
        Assert.Equal("MaterializedCompleteFamily", r.Status); Assert.Equal(2, r.Materialized); Assert.Equal(1, r.DistinctCells);
        Assert.Equal(1, r.AliasEntries); Assert.Equal(1, r.MatchedMidpointRecipes); Assert.Equal(2, output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Equal(0, r.Fights); Assert.Equal(0, r.NewSeeds);
    }

    [Fact]
    public async Task Invalid_entry_is_retained_and_blocks_complete_family_without_filtering_it_out()
    {
        var (r, output) = await Scan([Row(1), Row(2, Scenario with { FloorNumber = 6 })]);
        Assert.Equal("RetainedFamilyIssues", r.Status); Assert.Equal(2, r.Entries); Assert.Equal(1, r.Invalid);
        Assert.Contains("InvalidRetainedEntry", output); Assert.Single(r.Errors);
    }

    [Fact]
    public async Task Different_alias_participants_and_missing_required_recipe_block_complete_family()
    {
        var calls = 0; var (r, _) = await Scan([Row(1), Row(2)], prepare: (_, _) => Task.FromResult((new string('a', 64), Participants(++calls))));
        Assert.Equal("RetainedFamilyIssues", r.Status); Assert.Equal(1, r.Invalid);
        var missing = new Dictionary<string, (string, string)> { [new string('c', 64)] = ("missing", new string('d', 64)) };
        (r, _) = await Scan([Row(1), Row(2)], required: missing);
        Assert.Equal("RetainedFamilyIssues", r.Status); Assert.Equal(0, r.MatchedMidpointRecipes);
    }

    [Theory]
    [InlineData("short")] [InlineData("extra")] [InlineData("duplicate")] [InlineData("no-origins")]
    public async Task Missing_extra_duplicate_and_origin_free_entries_cannot_complete(string defect)
    {
        object[] rows = defect switch {
            "short" => [Row(1)], "extra" => [Row(1), Row(2), Row(3)], "duplicate" => [Row(1), Row(1)],
            _ => [Row(1), new { key = new string('a', 64), scenario = Scenario, sources = Array.Empty<object>() }] };
        await Assert.ThrowsAsync<InvalidDataException>(() => Scan(rows));
    }

    [Theory]
    [InlineData("combat")] [InlineData("cancel")] [InlineData("storage")]
    public async Task Combat_cancellation_and_resource_guards_stop_the_shared_loop(string defect)
    {
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<Exception>(() => Scan([Row(1), Row(2)], prepare: (_, ct) => {
            if (defect == "combat") TowerPerformanceTrace.BattleStarted();
            if (defect == "cancel") { cancel.Cancel(); ct.ThrowIfCancellationRequested(); }
            return Task.FromResult((new string('b', 64), Participants()));
        }, check: () => { if (defect == "storage") throw new IOException("Output cap."); }, ct: cancel.Token));
    }
}
