using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

// Fabricated scalar observations and journal events only: no generator, materialization or engine.
internal sealed class BalanceHarnessRefinementComparisonFixture(string mode = "complete") : ITowerRefinementComparisonRuntime
{
    internal readonly List<string> Calls = [];
    internal CancellationTokenSource? Cancel;
    internal static ExecutionIdentity SharedIdentity => new("fixture", "fixture", "fixture",
        new Dictionary<string, string> { ["fixture"] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(new byte[] { 1, 2, 3 })) });
    public void CreateSharedExecutable(string path, long bytes, CancellationToken ct)
        => TowerSharedExecutable.Create(path, SharedIdentity, bytes, (root, identity, limit, token) => {
            token.ThrowIfCancellationRequested(); if (limit < 3) throw new InvalidDataException("Fixture executable cap.");
            Directory.CreateDirectory(Path.Combine(root, "executable"));
            File.WriteAllBytes(Path.Combine(root, "executable/fixture.dll"), [1, 2, 3]);
            return new Dictionary<string, string> { ["fixture.dll"] = identity.AssemblyHashes["fixture"] };
        }, ct);
    public void VerifySharedExecutable(string path, CancellationToken ct) => TowerSharedExecutable.Verify(path, SharedIdentity, ct);
    private static void SharedReference(string path, TowerBulkOptions options, CancellationToken ct)
    {
        if (options.SharedExecutablePath is not null) {
            if (options.SharedExecutablePath != TowerSharedExecutable.RelativePath) throw new InvalidDataException("Fixture reference path.");
            TowerSharedExecutable.WriteReference(path, SharedIdentity, ct);
        }
    }
    private static void VerifyReference(string path, CancellationToken ct)
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(path)!, TowerSharedExecutable.Manifest)))
            TowerSharedExecutable.VerifyReference(path, SharedIdentity, ct);
    }
    internal static TowerBossDiscoveryDefinition[] Definitions()
    {
        var source = F.Definition(F.Input(candidates: 16, attempts: 16));
        source = source with { References = TowerRefinementComparisonModel.Controls.Select((id, i) => new BossBenchmarkReference(id, "fixture",
            TowerBossDiscovery.Scenario(source, "fixture", Choices()[i], []), "synthetic fixture", new string('d', 64))).ToArray() };
        var labels = new TowerRefinementComparisonSeeds([-1], [17], [101,102,103,104], Enumerable.Range(201,8).ToArray(), Enumerable.Range(301,32).ToArray());
        return TowerRefinementComparisonModel.Policies.Select(p => TowerRefinementComparisonModel.Definition(source, labels, p)).ToArray();
    }
    static PartyChoice[] Choices() => (from a in Enumerable.Range(0, 9) from b in Enumerable.Range(a + 1, 10 - a)
        from c in Enumerable.Range(b + 1, 11 - b) from e in Enumerable.Range(c + 1, 11 - c)
        select new[] { a, b, c, e }).Take(16).Select(ids => TowerPartySelection.Choice("synthetic",
            new Dictionary<int, IReadOnlyList<string>> { [1] = ids.Select(i => "e" + i.ToString("D2")).ToArray(), [2] = ["e04","e05","e06","e07"] })).ToArray();
    static BossDiscoveryRunReport Report(TowerBossDiscoveryDefinition definition, bool partial)
    {
        var d = TowerBossImprovement.Inputs(definition); var choices = Choices();
        var proposals = choices.Select((p, i) => new BossGeneratedProposal(new("proposal-" + i, 17, d.Generation.Methods[0],
            d.Generation.PolicyVersion == TowerTeamCoverageSearch.Version ? TowerTeamCoverageSearch.Operator : TowerDiscoveryRefinementSearch.FreshOperator,
            [], []), p, "synthetic", null, partial && i >= 14 ? "missing-team-roles" : "evaluated")).ToArray();
        var rows = choices.Take(partial ? 14 : 16).Select(p => {
            var cells = d.DiscoverySeeds.Select(s => new PartyFloorScore(s.Key, d.Floor, s.Value.Select(_ => false).ToArray(), 0,
                Convert.ToUInt32(p.Id[..6], 16) / (double)0xffffff * 100, 40, s.Value.Select(v => "trial-" + v).ToArray())).ToArray();
            return new BossDiscoveryMeasurement(p.Id, TowerBossGeneration.Fitness(d, cells, 100), cells, new(0,.5,0,0,0));
        }).ToArray();
        var status = partial ? "Incomplete" : "Complete";
        var generation = new BossGenerationResult(d.Generation.PolicyVersion, status, [new(d.Generation.Methods[0], 17,
            partial ? "ProposalBudgetExhausted" : "CandidateBudgetReached", proposals, rows)], choices.Take(rows.Length).ToArray(), null);
        return new(status, 64, rows.Length * 4, 0, generation, null);
    }
    void Charge(int count)
    {
        for (var i = 0; i < count; i++) {
            TowerPerformanceTrace.BattleStarted();
            if (mode == "cancel-completion") Cancel!.Cancel();
            TowerPerformanceTrace.BattleCompleted();
        }
    }
    public Task<BossDiscoveryRunReport> Discover(TowerBossDiscoveryDefinition d, string path, TowerBulkOptions options, CancellationToken token)
    {
        Calls.Add(Path.GetFileName(path)); Directory.CreateDirectory(path);
        SharedReference(path, options, token);
        if (options.RetryReserve != 0 || options.StorageAccounting != TowerStorageAccountant.Mode) throw new InvalidDataException("Changed fixture envelope.");
        if (mode == "storage") File.WriteAllBytes(Path.Combine(path,"oversize.bin"), new byte[4 * 1024 * 1024]);
        if (mode == "receipt") Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path)!, "discovery-gate.json"));
        var partial = mode == "partial-baseline" && Calls.Count == 1 || mode == "partial-candidate" && Calls.Count == 3;
        var result = Report(d, partial);
        Charge(mode == "overrun" ? 65 : mode == "undercharge" ? result.ActualBattles - 1 : result.ActualBattles);
        HarnessJson.WriteNew(Path.Combine(path, "report.json"), result); return Task.FromResult(result);
    }
    public Task<BossDiscoveryRunReport> VerifyDiscovery(TowerBossDiscoveryDefinition d, string path, CancellationToken token)
    {
        Calls.Add("verify-" + Path.GetFileName(path));
        VerifyReference(path, token);
        if (mode == "verification") throw new InvalidDataException("Injected archive verification failure.");
        var r = HarnessJson.Read<BossDiscoveryRunReport>(Path.Combine(path,"report.json"));
        return Task.FromResult(mode == "mismatch" ? r with { CacheHits = 1 } : r);
    }
    public Task Balance(TowerBalanceDefinition d, string path, TowerBulkOptions options, CancellationToken token)
    {
        Calls.Add(Path.GetFileName(path)); var root = Path.GetDirectoryName(path)!;
        if (!File.Exists(Path.Combine(root, "nominations.json")) || !File.Exists(Path.Combine(root, "discovery-gate.json"))
            || Path.GetFileName(path) == "confirmation" && !File.Exists(Path.Combine(root, "finalists.json")))
            throw new InvalidDataException("Selection was not durable before the next stage.");
        Directory.CreateDirectory(path); SharedReference(path, options, token); Charge(d.MaximumBattles);
        var evidence = d.Cells.Select(c => new TowerBalanceEvidence(c.Id, "Complete", HarnessJson.Hash(c.Scenario), HarnessJson.Hash(d.ContentHashes),
            d.SettingsHash, d.ExecutionHash, d.Cohorts.Single().RequiredPartySize,
            c.Scenario.Seeds.Select((s,j) => new TowerBalanceTrial(s, j < 4 ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), new string('a',64))).ToArray();
        if (mode == "partial-" + Path.GetFileName(path)) evidence = evidence[..^1];
        HarnessJson.WriteNew(Path.Combine(path,"evidence.json"), evidence); return Task.CompletedTask;
    }
    public Task<IReadOnlyList<TowerBalanceEvidence>> VerifyBalance(TowerBalanceDefinition d, string path, CancellationToken token)
    {
        Calls.Add("verify-" + Path.GetFileName(path));
        VerifyReference(path, token);
        return Task.FromResult<IReadOnlyList<TowerBalanceEvidence>>(HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(path,"evidence.json")));
    }
}
