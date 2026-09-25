using System.Buffers.Binary;
using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using S = BalanceHarness.TowerProposalStudy;

namespace BalanceHarness.ProcessFixture;

public sealed record ProposalOwnedFixture(string Source, string RegistryRoot, string PackageRoot, string Mode, string? Profile = null);

// Separate test executable. Public harness commands cannot select these operations.
public static class ProposalStudyFixtureHost
{
    public const string MatchedBaseline = "matched-placement-baseline-v1";
    public const string MatchedPlacement = "matched-placement-candidate-v1";
    public const string MatchedEvaluator = "placement-reference-draw-generated-victory-validation-odd5-even4-heldout-victory-v1";

    public static TowerProposalComparisonPlan FixturePlan(TowerProposalContext context, TowerProposalComparisonPlan source, string? profile)
    {
        S.Require(profile is null or MatchedBaseline or MatchedPlacement, "Unknown literal fixture profile.");
        if (profile is null) return TowerProposalComparison.Recreate(source, context);
        S.Require(context.Scope.RequiredPartySize == 10 && context.Scope.Budget.EssenceSlots == 5
            && context.Scope.Contexts.Single().CharacterTemplates.Count == 10
            && source.Version == S.LoadoutPlacementVersion, "Matched fixture requires the captured ten-actor/five-slot placement source.");
        var placement = TowerProposalComparison.CreateLoadoutPlacementPlan(context, source.Control);
        return profile == MatchedBaseline ? TowerProposalComparison.CreateAlliedActionPlan(context, placement.Control) : placement;
    }

    public static BattleOutcome LiteralOutcome(int searchRoot, bool validation, bool benchmark, bool drawReference, int validationIndex)
    {
        if (validation) return !benchmark && validationIndex >= 0 && validationIndex < (searchRoot % 2 == 1 ? 5 : 4)
            ? BattleOutcome.Victory : BattleOutcome.Draw;
        return searchRoot > 0 && drawReference ? BattleOutcome.Draw : BattleOutcome.Victory;
    }

    private static (LoadoutTrial Trial, int MaximumTicks) Binding(TowerPanelTrial request) =>
        (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
            new string('a', 64), new string('b', 64)), 1000);

    private static TowerBattleReport Report(TowerScenario scenario, int seed) => new(new BattleReport(1, scenario.Id, seed,
        FastCombatEngine.TicksPerSecond, JsonSerializer.SerializeToElement(new { literal = true }),
        new BattleSummary(BattleOutcome.Victory, BattleOutcome.Victory, "Victory", FastCombatEngine.TicksPerSecond, 1,
            [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry()), null), true, 0, 1);

    private static void Entropy(byte[] bytes)
    { for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4, 4), 200000+i); }

    private static object Prepare(ProposalOwnedFixture f)
    {
        var context = HarnessJson.Read<TowerProposalContext>(S.P(f.Source, "fixture-context.json"));
        S.Require(context.Scope.Contexts.Single().Id == "fixture" && context.Scope.ContentHashes.Values.All(h => h == HarnessJson.Hash(new { })),
            "Require literal empty-content fixture bindings.");
        context = context with { Scope = context.Scope with { ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()) } };
        var old = HarnessJson.Read<TowerProposalComparisonPlan>(S.P(f.Source, "fixture-plan.json"));
        var plan = FixturePlan(context, old, f.Profile);
        HarnessJson.WriteNew(S.P(f.PackageRoot, "context.json"), context);
        HarnessJson.WriteNew(S.P(f.PackageRoot, "plan.json"), plan);
        foreach (var name in new[] { "settings", "history" })
            File.Copy(S.P(f.Source, "fixture-" + name + ".json"), S.P(f.PackageRoot, name + ".json"));
        var history = HarnessJson.Read<int[]>(S.P(f.PackageRoot, "history.json"));
        HarnessJson.WriteNew(S.P(f.RegistryRoot, "prior-seed-ledger.json"), new { historical = history });
        foreach (var key in context.Scope.ContentHashes.Keys)
        {
            var path = S.P(f.PackageRoot, "content/Data/" + key); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "{}");
        }
        var runtime = TowerBossStudy.RetainExecutable(f.PackageRoot, ExecutionIdentity.Current(), 512L*1048576, default)
            .ToDictionary(p => p.Key, p => p.Value);
        // Real admission binds the producing PDB as well as runnable assets.
        // Include it here so owned fixtures cover exact retention of that manifest.
        var symbols = Path.Combine(Path.GetDirectoryName(typeof(S).Assembly.Location)!, "BalanceHarness.pdb");
        File.Copy(symbols, S.P(f.PackageRoot, "executable/BalanceHarness.pdb"));
        runtime.Add("BalanceHarness.pdb", HarnessJson.FileHash(symbols));
        var from = Path.GetFullPath(S.P(f.PackageRoot, "executable")); var to = Path.GetFullPath(S.P(f.PackageRoot, "runtime"));
        S.Require(Path.GetDirectoryName(from) == Path.GetFullPath(f.PackageRoot) && Path.GetDirectoryName(to) == Path.GetFullPath(f.PackageRoot), "Invalid fixture move.");
        Directory.Move(from, to); HarnessJson.WriteNew(S.P(f.PackageRoot, "runtime.json"), runtime);
        if (f.Profile is not null)
        {
            var bytes = new byte[65536]; Entropy(bytes);
            var allocation = S.Classify(bytes, history, plan.Version);
            var binding = TowerProposalComparison.Bind(plan, context, allocation.Selected);
            HarnessJson.WriteNew(S.P(f.PackageRoot, "matched-contract.json"), new {
                version = "tower-loadout-placement-matched-cost-fixtures-v1", evaluator = MatchedEvaluator,
                contextHash = HarnessJson.Hash(context), settingsSha256 = HarnessJson.FileHash(S.P(f.PackageRoot, "settings.json")),
                runtimeHash = HarnessJson.Hash(runtime), entropySha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)),
                selected = allocation.Selected, reserved = allocation.Reserved,
                commonV5PlanHashes = binding.Pairs.Select(p => HarnessJson.Hash(f.Profile == MatchedBaseline ? p.Candidate : p.Control)).ToArray()
            });
        }
        return new { status = "LiteralProposalFixturePrepared", productionEntropyDraws = 0, actualCombat = 0 };
    }

    private sealed class LiteralArchive
    {
        private readonly string root;
        private readonly string benchmark;
        private readonly int searchRoot;
        private readonly HashSet<string> placementReferences;
        public LiteralArchive(string root, string algorithm, ProposalStudyInputs inputs, CancellationToken ct, int searchRoot = 0, string? profile = null)
        {
            placementReferences = profile is not null || inputs.Plan.Version == S.LoadoutPlacementVersion
                ? inputs.Context.Scope.Starts.Select(s => s.Party.Id).ToHashSet() : [];
            this.root = root; benchmark = inputs.Plan.BenchmarkPartyId; this.searchRoot = searchRoot; Directory.CreateDirectory(root);
            foreach (var name in new[] { "recipes", "battles" }) Directory.CreateDirectory(S.P(root, name));
            foreach (var key in inputs.Context.Scope.ContentHashes.Keys)
            {
                ct.ThrowIfCancellationRequested(); var p = S.P(root, "content/Data/" + key);
                Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllText(p, "{}");
            }
            HarnessJson.WriteNew(S.P(root, "scope.json"), new LoadoutScope(algorithm, inputs.Settings, ExecutionIdentity.Current(), inputs.Context.Scope.ContentHashes, "gzip-json-v1"));
        }
        public (LoadoutTrial Trial, TowerBattleReport Report) Measure(TowerPanelTrial request)
        {
            var binding = Binding(request); var report = ProposalStudyFixtureHost.Report(request.Scenario, request.Seed);
            var outcome = LiteralOutcome(searchRoot, request.Role == TowerBenchmarkValidation.ValidationRole,
                request.PartyId == benchmark, placementReferences.Contains(request.PartyId), request.Scenario.Seeds.ToList().IndexOf(request.Seed));
            report = report with { Succeeded = outcome == BattleOutcome.Victory, Battle = report.Battle with {
                Summary = report.Battle.Summary with { ContentOutcome = outcome, EngineOutcome = outcome } } };
            var path = S.P(root, "recipes/" + binding.Trial.Recipe + ".json");
            if (!File.Exists(path)) HarnessJson.WriteNew(path, request.Scenario);
            TowerLoadoutArchive.WriteBattle(root, binding.Trial.Id, report, "gzip-json-v1");
            File.AppendAllText(S.P(root, "trials.jsonl"), JsonSerializer.Serialize(binding.Trial, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n");
            return (binding.Trial, report);
        }
        public void Seal(CancellationToken ct) => S.Seal(root, ct);
    }

    private static async Task<ProposalStudyResult> Study(string output, ProposalStudyInputs inputs, ExplorationReservation allocation,
        Action check, CancellationToken ct, string mode, string? profile)
    {
        Directory.CreateDirectory(S.P(output, "study"));
        using var attempts = new TowerPracticalSearch.Attempts(S.P(output, "attempts.jsonl"), 21888, check);
        LiteralArchive? heldout = null;
        var result = await S.Execute(inputs.Plan, inputs.Context, allocation.Selected, pair => TowerProposalComparison.ExecutePairAsync(inputs.Plan, pair,
            (_, _) => { }, async (arm, plan) => {
                var root = S.SearchRoot(output, pair.Root, arm);
                var archive = new LiteralArchive(root, TowerProposalRacingNative.ArchiveAlgorithm(plan), inputs, ct, pair.Root, profile);
                TowerPanelTrial? prepared = null;
                var search = await TowerProposalRacingNative.ExecuteAsync(plan, S.P(root, "racing"), 64L*1048576, request => {
                    if (mode == "attempt-failure") throw new IOException("Literal failure after durable attempt charge.");
                    prepared = request; return Binding(request);
                }, (_, _, _, _, _) => Task.FromResult(archive.Measure(prepared!)), check, ct, attempts.Event);
                if (search.Evaluation.Status == "Complete") archive.Seal(ct); return search;
            }), (freeze, request) => {
                heldout ??= new LiteralArchive(S.P(output, "heldout"), inputs.Plan.Version + "/heldout/" + HarnessJson.Hash(freeze), inputs, ct);
                check(); var binding = Binding(request); var row = heldout.Measure(request);
                return Task.FromResult(TowerAdaptiveRacingNative.Authenticate(request, binding.Trial, binding.MaximumTicks, row.Trial, row.Report));
            }, (name, value) => S.Save(output, "study/" + name, value, check), () => S.AttemptPrefix(S.P(output, "attempts.jsonl")), attempts.Event, ct);
        heldout!.Seal(ct); S.Seal(S.P(output, "study"), ct); return result;
    }

    private static async Task<ProposalStudyResult> Audit(string output, CancellationToken ct)
    {
        var q = HarnessJson.Read<ProposalStudyRequest>(S.P(output, "request.json")); var inputs = S.ReadInputs(q, output);
        var allocation = S.VerifyReservation(output, inputs);
        S.ValidateRuntime(S.P(output, "source/runtime.json"), S.P(output, "executable"), true);
        var heldout = S.P(output, "heldout"); var trials = TowerLoadoutArchive.Verify(heldout, ct); var index = 0;
        var result = await S.AuditStudy(output, inputs, allocation, (number, arm) => {
            var archive = S.SearchRoot(output, number, arm); var rows = TowerLoadoutArchive.Verify(archive, ct);
            return TowerProposalRacingNative.VerifyEvidenceAsync(S.P(archive, "racing"), Binding, rows,
                trial => TowerLoadoutArchive.ReadBattle(archive, trial.Id, "gzip-json-v1"), ct);
        }, request => {
            var binding = Binding(request); var trial = trials[index++];
            return TowerAdaptiveRacingNative.Authenticate(request, binding.Trial, binding.MaximumTicks, trial,
                TowerLoadoutArchive.ReadBattle(heldout, trial.Id, "gzip-json-v1"));
        }, ct);
        S.Require(index == trials.Count, "Extra literal held-out trials."); S.Match(output, "provisional-result.json", result); return result;
    }

    public static async Task<int> Run(string command, string path)
    {
        var f = HarnessJson.Read<ProposalOwnedFixture>(path);
        S.Require(Path.GetFileName(f.RegistryRoot).StartsWith("tower-proposal-owned-fixture-", StringComparison.Ordinal)
            && Path.GetDirectoryName(Path.GetFullPath(f.PackageRoot)) == Path.GetFullPath(f.RegistryRoot)
            && f.Mode is "complete" or "attempt-failure", "Require a separately named literal proposal fixture.");
        S.Require(f.Profile is null or MatchedBaseline or MatchedPlacement, "Unknown literal fixture profile.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Owned proposal fixture entered combat.")).Activate();
        var output = S.P(f.RegistryRoot, "result");
        var operations = new ProposalStudyOperations((q, ct) => {
            S.ValidateRequest(q, true); var inputs = S.ReadInputs(q);
            S.Require(inputs.Context.Scope.Contexts.Single().Id == "fixture"
                && inputs.Context.Scope.ContentHashes.Values.All(h => h == HarnessJson.Hash(new { })), "Real content is forbidden.");
            return inputs with { LiveHistory = TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, inputs.History, ct) };
        }, (o, i, a, check, ct) => Study(o, i, a, check, ct, f.Mode, f.Profile), Entropy);
        object result = command switch {
            "proposal-fixture-prepare" => Prepare(f),
            "proposal-fixture-run" => await S.RunOwned(output, default, operations),
            "proposal-fixture-audit" => await Audit(output, default),
            "proposal-fixture-publication-check" => S.PublicationCheck(output, true, default),
            "proposal-fixture-verify" => await S.Verify(output, File.ReadAllText(S.P(f.RegistryRoot, "closeout-pin.txt")).Trim(), default, Audit),
            _ => throw new InvalidDataException("Unknown proposal fixture command.")
        };
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
    }
}
