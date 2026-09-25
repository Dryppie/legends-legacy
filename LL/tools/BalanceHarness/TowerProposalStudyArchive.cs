using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerProposalStudy
{
    internal static string SearchRoot(string output, int root, string arm) => P(output, $"search/root-{root:D2}/{arm}");
    internal static void Save(string output, string name, object value, Action check)
    {
        check();
        var path = P(output, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var nested = TowerBulkCampaign.StorageBytes(output, default) - Directory.EnumerateFiles(output).Sum(p => new FileInfo(p).Length);
        new TowerCompleteReservation.Storage(output, NativeBytes - nested - 4*1048576).Put(name, value);
        check();
    }
    internal static void Seal(string root, CancellationToken ct)
    {
        var files = TowerBulkCampaign.Paths(root).ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), p => {
            ct.ThrowIfCancellationRequested(); return HarnessJson.FileHash(p);
        });
        HarnessJson.WriteNew(P(root, "files.json"), files);
    }

    private static TowerLoadoutArchive CreateArchive(string output, string root, string algorithm,
        ProposalStudyInputs inputs, int limit, Action check, CancellationToken ct)
    {
        Require(!Path.Exists(root), "No native archive retry."); Directory.CreateDirectory(root);
        foreach (var name in new[] { "recipes", "battles" }) Directory.CreateDirectory(P(root, name));
        check(); var hashes = TowerBundle.CopyContent(P(output, "content"), P(root, "content"), ct); check();
        var scope = new LoadoutScope(algorithm, inputs.Settings, ExecutionIdentity.Current(), hashes, "gzip-json-v1");
        Require(HarnessJson.Hash(hashes) == HarnessJson.Hash(inputs.Context.Scope.ContentHashes), "Changed copied archive content.");
        HarnessJson.WriteNew(P(root, "scope.json"), scope); return new(root, scope, limit);
    }

    internal static string AttemptPrefix(string path)
    {
        // The durable writer remains open for the held-out phase. A reader must
        // permit its existing write handle on Windows, while the writer itself
        // continues to deny any second writer.
        using var reader = TowerWorkAccounting.OpenText(path, FileShare.ReadWrite);
        var lines = new List<string>();
        while (lines.Count < 2*SearchFights && reader.ReadLine() is { } line) lines.Add(line);
        Require(lines.Count == 2*SearchFights, "Incomplete search attempt barrier.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n")));
    }

    internal static void VerifyAttempts(string output, ProposalStudyResult result, ProposalStudyFreeze freeze)
    {
        var path = P(output, "attempts.jsonl"); var lines = TowerWorkAccounting.ReadLines(path).ToArray();
        Require(lines.Length == 2*result.Fights && TowerWorkAccounting.ReadAllText(path).EndsWith('\n') && AttemptPrefix(path) == freeze.AttemptsHash,
            "Torn or changed attempt ledger/barrier.");
        for (var i = 0; i < result.Fights; i++)
        for (var j = 0; j < 2; j++)
            Require(HarnessJson.Hash(TowerWorkAccounting.Parse<JsonElement>(lines[2*i+j]))
                == HarnessJson.Hash(new { kind = j == 0 ? "Started" : "Completed", ordinal = i+1 }), "Reordered/unmatched attempt.");
    }

    internal static async Task<ProposalStudyResult> RunStudy(string output, ProposalStudyInputs inputs,
        ExplorationReservation allocation, Action check, CancellationToken ct)
    {
        Directory.CreateDirectory(P(output, "study"));
        var storage = inputs.EvidenceStorage is null ? null : new TowerProposalEvidenceStorage.Writer(output, inputs.EvidenceStorage, check, ct);
        TowerLoadoutArchive? heldout = null;
        using var attempts = new TowerPracticalSearch.Attempts(P(output, "attempts.jsonl"), inputs.Plan.MaximumFights, check);
        var result = await Execute(inputs.Plan, inputs.Context, allocation.Selected, async pair => {
            var a = CreateArchive(output, SearchRoot(output, pair.Root, "control"), TowerProposalRacingNative.ArchiveAlgorithm(pair.Control), inputs, 528, check, ct);
            var b = CreateArchive(output, SearchRoot(output, pair.Root, "candidate"), TowerProposalRacingNative.ArchiveAlgorithm(pair.Candidate), inputs, 528, check, ct);
            var search = await TowerProposalComparison.RunPairAsync(inputs.Plan, pair, a, b, 64L*1048576, check, ct, attempts.Event, storage);
            if (search.Status == "Complete") { Seal(a.OutputRoot, ct); Seal(b.OutputRoot, ct); }
            return search;
        }, async (freeze, request) => {
            heldout ??= CreateArchive(output, P(output, "heldout"), inputs.Plan.Version + "/heldout/" + HarnessJson.Hash(freeze), inputs, 12*3*256, check, ct);
            check(); var input = heldout.Materialize(request.Scenario, request.Seed);
            var arm = heldout.CapturedScope.Algorithm;
            var expected = new LoadoutTrial($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
                HarnessJson.Hash(input), TowerLoadoutArchive.Key(heldout.CapturedScope, arm, input));
            var response = await heldout.EvaluateAsync(arm, request.Role, request.Scenario, request.Seed, ct);
            Require(heldout.CacheHits == 0, "Unexpected held-out cache hit.");
            var outcome = TowerAdaptiveRacingNative.Authenticate(request, expected, input.Rules.MaxTicks, response.Trial, response.Report);
            check(); return outcome;
        }, (name, value) => {
            if (storage is not null && TowerProposalEvidenceStorage.Eligible(name)) storage.Put(P(output, "study"), name, value);
            else Save(output, "study/" + name, value, check);
        }, () => AttemptPrefix(P(output, "attempts.jsonl")), attempts.Event, ct);
        Require(attempts.Started == attempts.Completed && attempts.Completed == result.Fights && heldout is not null, "Changed complete-study accounting.");
        if (storage is not null)
        {
            storage.SealDirectory(P(output, "study"), TowerProposalEvidenceStorage.Names(HarnessJson.Read<ProposalStudyFreeze>(P(output, "study/freeze.json"))),
                (name, value) => Save(output, "study/" + name, value, check));
            storage.Finish();
        }
        Seal(heldout!.OutputRoot, ct); Seal(P(output, "study"), ct); check(); return result;
    }

    internal static async Task<ProposalStudyResult> AuditStudy(string output, ProposalStudyInputs inputs,
        ExplorationReservation allocation, Func<int, string, Task<TowerProposalRacingReport>> verifySearch,
        Func<TowerPanelTrial, TowerPanelOutcome> heldout, CancellationToken ct, TowerProposalEvidenceStorage.Reader? storage = null)
    {
        storage ??= TowerProposalEvidenceStorage.Open(output, inputs.EvidenceStorage, ct);
        TowerBulkCampaign.VerifyFiles(P(output, "study"), "files.json", true, ct);
        var pairIndex = 0;
        var result = await Execute(inputs.Plan, inputs.Context, allocation.Selected, async pair => {
            Require(pair.Root == ++pairIndex, "Reordered pair.");
            var control = await verifySearch(pair.Root, "control"); var candidate = await verifySearch(pair.Root, "candidate");
            TowerWorkAccounting.Add("reconstructedRoots");
            return new(HarnessJson.Hash(inputs.Plan), HarnessJson.Hash(pair), "Complete", control, candidate);
        }, (_, request) => Task.FromResult(heldout(request)), (name, value) => {
            if (storage is not null && TowerProposalEvidenceStorage.Eligible(name))
                Require(HarnessJson.Hash(storage.Read<JsonElement>(P(output, "study"), name, ct)) == HarnessJson.Hash(value), "Changed encoded study artifact: " + name);
            else Match(P(output, "study"), name, value);
            if (name.StartsWith("placement-catalogue-", StringComparison.Ordinal)) TowerWorkAccounting.Add("reconstructedCatalogues");
            if (name.StartsWith("heldout-", StringComparison.Ordinal)) TowerWorkAccounting.Add("reconstructedHeldoutMembers");
        },
            () => AttemptPrefix(P(output, "attempts.jsonl")), _ => { }, ct);
        var freeze = HarnessJson.Read<ProposalStudyFreeze>(P(output, "study/freeze.json"));
        VerifyAttempts(output, result, freeze);
        var expected = new[] { "binding.json", "freeze.json", "summary.json", "files.json" }
            .Concat(Enumerable.Range(1, 12).Select(i => $"pair-{i:D2}.json"))
            .Concat(inputs.Plan.Version == LoadoutPlacementVersion ? Enumerable.Range(1, 12).Select(i => $"placement-catalogue-{i:D2}.json") : [])
            .Concat(freeze.Families.SelectMany(f => f.Members.Select(m => $"heldout-{f.Root:D2}-{m.RecipeHash}.json"))).Order(StringComparer.Ordinal);
        var physicalExpected = storage is null ? expected : storage.PhysicalMembers(P(output, "study"), expected).Order(StringComparer.Ordinal);
        Require(Directory.EnumerateFileSystemEntries(P(output, "study")).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(physicalExpected), "Changed study evidence membership.");
        storage?.Finish();
        TowerWorkAccounting.Add("reconstructedStudyEndpoints");
        return result;
    }

    internal static async Task<ProposalStudyResult> Audit(string output, CancellationToken ct)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit cannot fight.")).Activate();
        var q = TowerContractJson.Read<ProposalStudyRequest>(P(output, "request.json")); ValidateRequest(q, false);
        Require(!File.Exists(P(output, "failure.json")), "Incomplete studies cannot publish.");
        var inputs = ReadInputs(q, output); var allocation = VerifyReservation(output, inputs);
        TowerProposalEvidenceStorage.VerifyModules(output, q.EvidenceStorage);
        var storage = TowerProposalEvidenceStorage.Open(output, q.EvidenceStorage, ct);
        ValidateContent(inputs, P(output, "content"), ct);
        ValidateRuntime(P(output, "source/runtime.json"), P(output, "executable"), true);
        var freeze = HarnessJson.Read<ProposalStudyFreeze>(P(output, "study/freeze.json"));
        var archive = P(output, "heldout"); var trials = TowerLoadoutArchive.Verify(archive, ct);
        var scope = HarnessJson.Read<LoadoutScope>(P(archive, "scope.json"));
        Require(scope.Algorithm == inputs.Plan.Version + "/heldout/" + HarnessJson.Hash(freeze) && scope.ReportStorage == "gzip-json-v1"
            && HarnessJson.Hash(scope.Settings) == inputs.Context.Scope.SettingsHash && HarnessJson.Hash(scope.Execution) == inputs.Context.Scope.ExecutionHash
            && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(inputs.Context.Scope.ContentHashes), "Changed held-out scope.");
        Require(HarnessJson.Hash(TowerCompactBundle.ContentHashes(P(archive, "content"), ct)) == HarnessJson.Hash(scope.ContentHashes), "Changed held-out content.");
        var runner = new TowerBattleRunner(P(archive, "content"), new OfflineContent(P(archive, "content"), scope.Settings.Threat));
        var index = 0;
        var result = await AuditStudy(output, inputs, allocation, (root, arm) => {
            var folder = SearchRoot(output, root, arm);
            return TowerProposalRacingNative.VerifyAsync(folder, HarnessJson.FileHash(P(folder, "files.json")), ct, storage);
        }, request => {
            Require(index < trials.Count, "Missing held-out trial.");
            var trial = trials[index++]; var input = runner.CreateInput(request.Scenario, request.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
            var expected = new LoadoutTrial($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
                HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, scope.Algorithm, input));
            Match(archive, "recipes/" + expected.Recipe + ".json", request.Scenario);
            var outcome = TowerAdaptiveRacingNative.Authenticate(request, expected, input.Rules.MaxTicks, trial, TowerLoadoutArchive.ReadBattle(archive, trial.Id, scope.ReportStorage));
            TowerWorkAccounting.Add("reconstructedTrialBindings");
            return outcome;
        }, ct, storage);
        Require(index == trials.Count && index == result.HeldoutFights && TowerWorkAccounting.ReadAllText(P(archive, "trials.jsonl")).EndsWith('\n'), "Extra or torn held-out trials.");
        Require(Directory.EnumerateFileSystemEntries(P(archive, "recipes")).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .SequenceEqual(trials.Select(t => t.Recipe + ".json").Distinct().Order(StringComparer.Ordinal))
            && Directory.EnumerateFileSystemEntries(P(archive, "battles")).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .SequenceEqual(trials.Select(t => t.Id + ".json.gz").Order(StringComparer.Ordinal)), "Changed held-out recipe/battle membership.");
        Match(output, "provisional-result.json", result); return result;
    }
}
