using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompleteSetupTests
{
    [Theory]
    [InlineData("first", 0, -106593242)]
    [InlineData("first", 1, -391503819)]
    [InlineData("second", 0, 1801284466)]
    public void Allocator_encoding_matches_independent_fixture_vectors(string label, int ordinal, int expected)
    {
        // Separate fixture domain/master; never derive a value in the prospective balance domain.
        Assert.Equal(expected, TowerCompleteFamilySetup.Candidate("complete-family-fixture", 7, label, ordinal));
    }

    [Fact]
    public void Allocation_rejects_history_duplicates_and_previous_stage_values_in_order()
    {
        var used = new HashSet<int> { 10, 20 }; int[] candidates = [10, 30, 30, 40];
        var first = TowerCompleteFamilySetup.Draw("fixture", 0, "first", 2, 4, used, default, i => candidates[i]);
        Assert.Equal(new[] { 30, 40 }, first);
        candidates = [40, 20, 50, 60];
        var second = TowerCompleteFamilySetup.Draw("fixture", 0, "second", 2, 4, used, default, i => candidates[i]);
        Assert.Equal(new[] { 50, 60 }, second); Assert.Equal(6, used.Count);
    }

    [Fact]
    public void Exhaustion_preserves_partial_acceptance_and_cancellation_stops_before_derivation()
    {
        var used = new HashSet<int> { 10 };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilySetup.Draw("fixture", 0, "first", 2, 3, used, default, _ => 20));
        Assert.Contains(20, used);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); var calls = 0;
        Assert.Throws<OperationCanceledException>(() => TowerCompleteFamilySetup.Draw("fixture", 0, "first", 1, 1, used,
            cancellation.Token, _ => { calls++; return 30; })); Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("algorithm")] [InlineData("domain")] [InlineData("master")]
    [InlineData("first")] [InlineData("second")] [InlineData("limit")]
    public void Prospective_allocator_binding_rejects_changed_parameters(string change)
    {
        var p = TowerCompleteFamilySetup.Allocator; TowerCompleteFamilySetup.ValidateAllocator(p);
        p = change switch { "algorithm" => p with { Algorithm = "other" }, "domain" => p with { Domain = "other" },
            "master" => p with { Master = 0 }, "first" => p with { FirstCount = 33 },
            "second" => p with { SecondCount = 255 }, _ => p with { MaximumCandidatesPerStage = 100001 } };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilySetup.ValidateAllocator(p));
    }

    [Fact]
    public void Complete_history_retains_unused_values_and_deduplicates_equal_files()
    {
        using var t = new Temp(); t.Write("prior-seed-ledger.json", new { unused = new[] { 1, 2 } });
        t.Write("history-input.json", new { unused = new[] { 1, 2 } });
        t.Write("seed-ledger.json", new { historical = new[] { 1, 2 }, shared = new[] { 3 } });
        var files = t.Files(); var r = TowerCompleteFamilySetup.History(t.Root, t.Output, files, files, t.P("seed-ledger.json"), default);
        Assert.Equal(3, r.Files); Assert.Equal(2, r.DistinctHashes); Assert.Equal(new[] { 1, 2, 3 }, r.Values);
        Assert.Equal(files.Keys.Sum(p => new FileInfo(p).Length), r.Bytes);
    }

    [Theory]
    [InlineData("new-file")] [InlineData("missing-file")] [InlineData("tampered")]
    [InlineData("lost-required")] [InlineData("incomplete-authority")] [InlineData("unbound-authority")]
    public void Setup_refuses_stale_or_incomplete_history(string change)
    {
        using var t = new Temp(); t.Write("prior-seed-ledger.json", new[] { 1, 2 });
        t.Write("seed-ledger.json", new[] { 1, 2, 3 }); var files = t.Files(); var required = new Dictionary<string, string>(files);
        var authority = t.P("seed-ledger.json");
        switch (change)
        {
            case "new-file": t.Write("history-input.json", new[] { 4 }); break;
            case "missing-file": files.Remove(t.P("prior-seed-ledger.json")); break;
            case "tampered": File.AppendAllText(t.P("prior-seed-ledger.json"), " "); break;
            case "lost-required": required[t.P("lost.json")] = HarnessJson.Hash("lost"); break;
            case "incomplete-authority": authority = t.P("prior-seed-ledger.json"); break;
            case "unbound-authority": authority = t.P("absent.json"); break;
        }
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilySetup.History(t.Root, t.Output, required, files, authority, default));
    }

    [Fact]
    public async Task Setup_refuses_overwrite_and_missing_allocator_before_loading_any_source()
    {
        using var t = new Temp(); var output = t.P("result.json"); File.WriteAllText(output, "preserve");
        await Assert.ThrowsAsync<IOException>(() => TowerCompleteFamilySetup.Inspect(t.P("absent.json"), output));
        Assert.Equal("preserve", File.ReadAllText(output));
        t.Write("request.json", new TowerCompleteSetupRequest(t.P("absent"), null!, "absent", new Dictionary<string, string>()));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompleteFamilySetup.Inspect(t.P("request.json"), t.P("new.json")));
        Assert.False(File.Exists(t.P("new.json")));
    }

    private sealed class Temp : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "tower-complete-setup-" + Guid.NewGuid().ToString("N"));
        public string Output => P("current");
        public Temp() { Directory.CreateDirectory(Root); Directory.CreateDirectory(Output); }
        public string P(string name) => Path.Combine(Root, name);
        public void Write(string name, object value) => HarnessJson.WriteNew(P(name), value);
        public Dictionary<string, string> Files() => TowerCompleteFamilyInputs.HistoryRegistry(Root, Output, default)
            .ToDictionary(p => p, HarnessJson.FileHash);
        public void Dispose()
        {
            var full = Path.GetFullPath(Root);
            if (!full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("tower-complete-setup-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected fixture cleanup path.");
            Directory.Delete(full, true);
        }
    }
}
