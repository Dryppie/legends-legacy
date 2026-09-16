using System.IO.Compression;
using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessRetainedStandaloneTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Standalone parity entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static string Repository()
    {
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "LL/tools/BalanceHarness/BalanceHarness.csproj"))) return d.FullName;
        throw new DirectoryNotFoundException("Repository not found.");
    }
    private static (TowerBossDiscoveryDefinition Definition, BossGenerationResult Report) Saved(string name)
    {
        // Committed snapshots keep these regression tests independent of ignored experiment output.
        using var archive = ZipFile.OpenRead(Path.Combine(Repository(), "LL/tests/EssenceSystem.Tests/Fixtures/retained-composition-parity.zip"));
        T Read<T>(string suffix)
        {
            using var stream = (archive.GetEntry(name + suffix) ?? throw new InvalidDataException("Missing parity fixture: " + name + suffix)).Open();
            return JsonSerializer.Deserialize<T>(stream, HarnessJson.Options) ?? throw new InvalidDataException("Empty parity fixture.");
        }
        return (Read<TowerBossDiscoveryDefinition>("-definition.json"), Read<BossGenerationResult>("-report.json"));
    }
    private static TowerBossDiscoveryDefinition Independent(TowerBossDiscoveryDefinition d) => d with {
        Mode = TowerBossDiscovery.Independent, Starts = [],
        Generation = d.Generation with { PolicyVersion = F.Policy, Methods = [F.Method] }
    };
    private static TowerBossDiscoveryDefinition Prepare(TowerBossDiscoveryDefinition source) =>
        TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray(), TowerSuppliedCompositionSearch.StandaloneVersion);

    [Theory]
    [InlineData("additive-map-0")] [InlineData("additive-map-1")] [InlineData("additive-map-2")]
    [InlineData("within-map-0")] [InlineData("within-map-1")] [InlineData("within-map-2")]
    [InlineData("cross-map-0")] [InlineData("cross-map-1")] [InlineData("cross-map-2")]
    [InlineData("two-starts")]
    public async Task Standalone_matches_every_saved_comparator_proposal_and_measurement(string name)
    {
        var saved = Saved(name); var source = Independent(saved.Definition); var sourceBefore = Json(source);
        var definition = Prepare(source); Assert.Equal(sourceBefore, Json(source));
        Assert.Equal(new[] { TowerSuppliedCompositionSearch.Baseline }, definition.Generation.Methods);
        var input = TowerBossImprovement.Inputs(definition);
        var old = saved.Report.Arms.Where(a => a.Method == TowerSuppliedCompositionSearch.Baseline).ToArray();
        var measurements = old.SelectMany(a => a.Evaluations).GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
        // Reuse recorded scalar measurements only. An unrecorded recipe fails;
        // every recorded proposal still has to match, including rejection order.
        var result = await TowerBossImprovement.ExecuteAsync(definition, input, F.Mechanics(input),
            (party, _, token) => { token.ThrowIfCancellationRequested(); return Task.FromResult(measurements[party.Id]); }, default);
        Assert.Equal("Complete", result.Status); Assert.Equal(TowerSuppliedCompositionSearch.StandaloneVersion, result.Version);
        Assert.Equal(3, result.Arms.Count);
        foreach (var arm in result.Arms) {
            Assert.Equal(TowerSuppliedCompositionSearch.Baseline, arm.Method);
            Assert.Equal(Json(old.Single(a => a.Seed == arm.Seed)), Json(arm));
            Assert.DoesNotContain(arm.Proposals, p => p.Provenance.Operator is "order" or "essence-block" or "character-block" or "donor-block");
            Assert.All(arm.Proposals.Where(p => p.Result == "evaluated"), p => {
                TowerBossDiscovery.ValidateParty(definition, p.Party!);
                Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            });
        }
        Assert.Equal(definition.Stages.Shortlist, result.DiscoveryShortlist.Count);
        Assert.All(result.DiscoveryShortlist, p => Assert.Contains(p.Id, measurements.Keys));
        TowerBossDiscovery.ValidateProvenance(definition, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        Assert.Contains("Explicit supplied references were scored", TowerBossDiscoveryRun.Markdown(new("Complete", 0, 0, 0, result, null)));
        var output = Environment.GetEnvironmentVariable("LL_STANDALONE_OUTPUT");
        if (!string.IsNullOrEmpty(output)) {
            Directory.CreateDirectory(output); HarnessJson.WriteNew(Path.Combine(output, name + "-standalone.json"), result);
        }
    }

    [Fact]
    public void Versioned_method_lists_and_default_preparation_remain_exact()
    {
        var source = Independent(Saved("two-starts").Definition); var standalone = Prepare(source);
        var paired = TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray());
        Assert.Equal(TowerSuppliedCompositionSearch.Version, paired.Generation.PolicyVersion);
        Assert.Equal(TowerSuppliedCompositionSearch.Methods, paired.Generation.Methods);
        TowerBossGeneration.ValidateInputs(TowerBossImprovement.Inputs(standalone));
        Assert.True(TowerCompositionSearch.IsCompositionOnly(standalone.Generation.PolicyVersion));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(standalone with {
            Generation = standalone.Generation with { Methods = TowerSuppliedCompositionSearch.Methods }
        }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(paired with {
            Generation = paired.Generation with { Methods = [TowerSuppliedCompositionSearch.Baseline] }
        }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(standalone with { Mode = TowerBossDiscovery.Independent, Starts = [] }));
        var start = standalone.Starts[0];
        var root = new BossDiscoveryProvenance("root", 17, TowerSuppliedCompositionSearch.Baseline, "supplied", [start.Id], [start.ReferenceId]);
        TowerBossDiscovery.ValidateProvenance(standalone, [root]);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(standalone, [root with { Method = TowerSuppliedCompositionSearch.Block }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(standalone, [root, new("child", 17, TowerSuppliedCompositionSearch.Baseline, "order", ["root"], root.ReferenceIds)]));
    }

    [Fact]
    public async Task Standalone_exhaustion_and_cancellation_keep_the_existing_budget_contract()
    {
        var input = F.Input(owners: 1, poolSize: 4, candidates: 8, attempts: 24); var source = F.Definition(input);
        var party = TowerPartySelection.Choice("fixture", new Dictionary<int, IReadOnlyList<string>> { [1] = ["e00", "e01", "e02", "e03"] });
        source = source with { References = [new("saved", "fixture", TowerBossDiscovery.Scenario(source, "fixture", party, []), "Synthetic", new string('d', 64))] };
        var d = Prepare(source); var prepared = TowerBossImprovement.Inputs(d);
        Task<BossGenerationResult> Run(CancellationToken token) => TowerBossImprovement.ExecuteAsync(d, prepared, F.Mechanics(prepared),
            (p, _, _) => Task.FromResult(F.Measure(prepared, p)), token);
        var result = await Run(default); Assert.Equal("Incomplete", result.Status);
        var arm = Assert.Single(result.Arms); Assert.Single(arm.Evaluations); Assert.Equal(24, arm.Proposals.Count); Assert.Empty(result.DiscoveryShortlist);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        var cancelled = await Run(stop.Token); Assert.Equal("Cancelled", cancelled.Status); Assert.Empty(cancelled.DiscoveryShortlist);
        Assert.All(cancelled.Arms, a => Assert.Empty(a.Proposals));
    }
}
