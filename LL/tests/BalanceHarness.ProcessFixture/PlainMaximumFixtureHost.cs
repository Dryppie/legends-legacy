using System.Buffers.Binary;
using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using S = BalanceHarness.TowerProposalStudy;

namespace BalanceHarness.ProcessFixture;

// Test executable only. No public harness command can select literal outcomes.
public static class PlainMaximumFixtureHost
{
    public const string Profile = "plain-maximum-placement-v1";
    public const string NominationProfile = "plain-maximum-nomination-v1";
    private static readonly JsonSerializerOptions Lines = new(HarnessJson.Options) { WriteIndented = false };

    public static BattleOutcome Outcome(bool heldout, bool reference, bool validation, int index) =>
        heldout || !reference && (!validation || index is >= 0 and < 5) ? BattleOutcome.Victory : BattleOutcome.Draw;

    public static BattleOutcome NominationOutcome(bool heldout, bool reference, bool benchmark, bool nomination, bool validation, int index) =>
        heldout || (validation ? !benchmark && index is >= 0 and < 5 : nomination ? reference && !benchmark : !reference)
            ? BattleOutcome.Victory : BattleOutcome.Draw;

    private static void Entropy(byte[] bytes)
    {
        S.Require(bytes.Length == S.EntropyWords * 4, "Changed literal batch length.");
        for (var i = 0; i < S.EntropyWords; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), 1000000000 + i);
    }

    public static ExplorationReservation LiteralAllocation(TowerProposalContext context)
    {
        var bytes = new byte[S.EntropyWords * 4]; Entropy(bytes);
        return S.Classify(bytes, S.LegacySeeds(context).Concat(context.Scope.ExcludedCombatSeeds).Distinct().Order().ToArray(), S.LoadoutPlacementVersion);
    }

    public static (TowerProposalContext Context, TowerProposalComparisonPlan Plan, int[] History) BindHistory(
        TowerProposalContext context, TowerProposalComparisonPlan plan)
    {
        var legacy = S.LegacySeeds(context);
        var history = legacy.Concat(context.Scope.ExcludedCombatSeeds).Concat(context.Scope.Generation.Seeds)
            .Append(context.RootSeed).Concat(context.Scope.References.SelectMany(r => r.Scenario.Seeds)).Distinct().Order().ToArray();
        context = context with { Scope = context.Scope with { ExcludedCombatSeeds = history.Except(legacy).Order().ToArray() } };
        return (context, TowerProposalComparison.Recreate(plan, context), history);
    }

    private static object Prepare(ProposalOwnedFixture f)
    {
        var context = HarnessJson.Read<TowerProposalContext>(S.P(f.Source, "runtime-context.json"));
        var plan = HarnessJson.Read<TowerProposalComparisonPlan>(S.P(f.Source, "runtime-plan.json"));
        var settings = HarnessJson.Read<TowerSettings>(S.P(f.Source, "settings.json"));
        S.Require(plan.Version == (f.Profile == NominationProfile ? S.NominationVersion : S.LoadoutPlacementVersion) && context.Scope.RequiredPartySize == 10
            && context.Scope.Budget.EssenceSlots == 5, "Require qualified placement inputs.");
        S.Require(HarnessJson.Hash(plan) == HarnessJson.Hash(TowerProposalComparison.Recreate(plan, context)), "Changed qualified plan.");
        int[] history;
        (context, plan, history) = BindHistory(context, plan);
        var inputs = new ProposalStudyInputs(plan, context, settings, history, new(new Dictionary<string, string>(), history));
        S.ValidateContent(inputs, S.P(f.Source, "content"), default);
        S.ValidateRuntime(S.P(f.Source, "runtime.json"), AppContext.BaseDirectory, false);
        HarnessJson.WriteNew(S.P(f.PackageRoot, "context.json"), context);
        HarnessJson.WriteNew(S.P(f.PackageRoot, "plan.json"), plan);
        foreach (var (source, target) in new[] { ("settings", "settings"), ("runtime", "runtime") })
            File.Copy(S.P(f.Source, source + ".json"), S.P(f.PackageRoot, target + ".json"));
        HarnessJson.WriteNew(S.P(f.PackageRoot, "history.json"), history);
        HarnessJson.WriteNew(S.P(f.RegistryRoot, "prior-seed-ledger.json"), new { historical = history });
        TowerBundle.CopyContent(S.P(f.Source, "content"), S.P(f.PackageRoot, "content"), default);
        S.RetainRuntime(S.P(f.PackageRoot, "runtime.json"), f.PackageRoot, 512L * 1048576, default);
        Directory.Move(S.P(f.PackageRoot, "executable"), S.P(f.PackageRoot, "runtime"));
        return new { status = "PlainMaximumFixturePrepared", fixtureOnly = true, actualCombat = 0, productionEntropyDraws = 0 };
    }

    private sealed class Archive
    {
        private readonly string root;
        private readonly LoadoutScope scope;
        private readonly TowerBattleRunner runner;
        private readonly bool heldout;
        private readonly HashSet<string> references;
        private readonly bool generatedNomination;
        private readonly string benchmark;
        public Archive(string root, string algorithm, ProposalStudyInputs inputs, string content, bool heldout, CancellationToken ct)
        {
            this.root = root; this.heldout = heldout;
            references = inputs.Context.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
            generatedNomination = inputs.Plan.Version == S.NominationVersion;
            benchmark = inputs.Context.Scope.Starts.Single(s => s.ReferenceId == inputs.Context.BenchmarkReferenceId).Party.Id;
            Directory.CreateDirectory(S.P(root, "recipes")); Directory.CreateDirectory(S.P(root, "battles"));
            var hashes = TowerBundle.CopyContent(content, S.P(root, "content"), ct);
            scope = new(algorithm, inputs.Settings, ExecutionIdentity.Current(), hashes, "gzip-json-v1");
            HarnessJson.WriteNew(S.P(root, "scope.json"), scope);
            runner = new(S.P(root, "content"), new OfflineContent(S.P(root, "content"), inputs.Settings.Threat));
        }
        public (LoadoutTrial Trial, int MaximumTicks) Bind(TowerPanelTrial request)
        {
            var input = runner.CreateInput(request.Scenario, request.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
            var arm = heldout ? scope.Algorithm : TowerProposalRacingNative.Arm(scope.Algorithm, request);
            return (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
                HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, arm, input)), input.Rules.MaxTicks);
        }
        public (LoadoutTrial Trial, TowerBattleReport Report) Write(TowerPanelTrial request, LoadoutTrial trial)
        {
            var index = request.Scenario.Seeds.ToList().IndexOf(request.Seed);
            var outcome = generatedNomination
                ? NominationOutcome(heldout, references.Contains(request.PartyId), request.PartyId == benchmark,
                    request.Role == TowerBenchmarkValidation.NominationRole, request.Role == TowerBenchmarkValidation.ValidationRole, index)
                : Outcome(heldout, references.Contains(request.PartyId), request.Role == TowerBenchmarkValidation.ValidationRole, index);
            var report = new TowerBattleReport(new BattleReport(1, request.Scenario.Id, request.Seed, FastCombatEngine.TicksPerSecond,
                JsonSerializer.SerializeToElement(new { literal = true }), new BattleSummary(outcome, outcome, outcome.ToString(),
                    FastCombatEngine.TicksPerSecond, 1, [new SimpleCombatEntity("fixture", "fixture", "", 10, 0)], [], [], new CompactCombatTelemetry()), null),
                outcome == BattleOutcome.Victory, 0, 1);
            var recipe = S.P(root, "recipes/" + trial.Recipe + ".json");
            if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, request.Scenario);
            TowerLoadoutArchive.WriteBattle(root, trial.Id, report, "gzip-json-v1");
            File.AppendAllText(S.P(root, "trials.jsonl"), JsonSerializer.Serialize(trial, Lines) + "\n");
            return (trial, report);
        }
        public void Seal(CancellationToken ct) => S.Seal(root, ct);
    }

    private static object Inspect(ProposalOwnedFixture f)
    {
        var inputs = S.Inspect(HarnessJson.Read<ProposalStudyRequest>(S.P(f.PackageRoot, "request.json")), default);
        return new { status = "ProductionInspectionPassed", contextHash = HarnessJson.Hash(inputs.Context), historyValues = inputs.History.Length };
    }

    private static async Task<ProposalStudyResult> Study(string output, ProposalStudyInputs inputs, ExplorationReservation allocation, Action check, CancellationToken ct)
    {
        Directory.CreateDirectory(S.P(output, "study"));
        using var attempts = new TowerPracticalSearch.Attempts(S.P(output, "attempts.jsonl"), inputs.Plan.MaximumFights, check);
        Archive? heldout = null; var completed = 0;
        var result = await S.Execute(inputs.Plan, inputs.Context, allocation.Selected, pair => TowerProposalComparison.ExecutePairAsync(inputs.Plan, pair,
            (_, _) => { }, async (arm, plan) => {
                var root = S.SearchRoot(output, pair.Root, arm);
                var archive = new Archive(root, TowerProposalRacingNative.ArchiveAlgorithm(plan), inputs, S.P(output, "content"), false, ct);
                TowerPanelTrial current = null!; (LoadoutTrial Trial, int MaximumTicks) binding = default;
                var search = await TowerProposalRacingNative.ExecuteAsync(plan, S.P(root, "racing"), 64L * 1048576,
                    r => { current = r; binding = archive.Bind(r); return binding; },
                    (_, _, _, _, _) => Task.FromResult(archive.Write(current, binding.Trial!)), check, ct, attempts.Event);
                S.Require(search.Evaluation.Status == "Complete", "Incomplete literal search."); archive.Seal(ct); completed++; return search;
            }), (freeze, request) => {
                S.Require(completed == 24 && freeze.Families.Count == 12 && freeze.Families.All(f => f.Members.Count == 3),
                    "Maximum fixture requires all 24 frozen outputs and three distinct members at every root.");
                heldout ??= new Archive(S.P(output, "heldout"), inputs.Plan.Version + "/heldout/" + HarnessJson.Hash(freeze), inputs, S.P(output, "content"), true, ct);
                check(); var binding = heldout.Bind(request); var row = heldout.Write(request, binding.Trial);
                return Task.FromResult(TowerAdaptiveRacingNative.Authenticate(request, binding.Trial, binding.MaximumTicks, row.Trial, row.Report));
            }, (name, value) => S.Save(output, "study/" + name, value, check), () => S.AttemptPrefix(S.P(output, "attempts.jsonl")), attempts.Event, ct);
        S.Require(result.SearchFights == 12672 && result.HeldoutFights == 9216 && result.Fights == 21888, "Maximum workload not reached.");
        heldout!.Seal(ct); S.Seal(S.P(output, "study"), ct); return result;
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            S.Require(args.Length == 2, "Require command and isolated fixture declaration.");
            var f = HarnessJson.Read<ProposalOwnedFixture>(args[1]);
            if (f.Profile != Profile && f.Profile != NominationProfile) return await ProposalStudyFixtureHost.Run(args[0], args[1]);
            S.Require(Path.GetFileName(f.RegistryRoot).StartsWith("tower-proposal-owned-fixture-", StringComparison.Ordinal)
                && Path.GetDirectoryName(Path.GetFullPath(f.PackageRoot)) == Path.GetFullPath(f.RegistryRoot)
                && f.Mode == "complete", "Require isolated maximum fixture.");
            using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Maximum fixture entered combat.")).Activate();
            var output = S.P(f.RegistryRoot, "result");
            var operations = new ProposalStudyOperations(S.Inspect, Study, Entropy);
            object result = args[0] switch {
                "proposal-fixture-prepare" => Prepare(f),
                "proposal-fixture-inspect" => Inspect(f),
                "proposal-fixture-run" => await S.RunOwned(output, default, operations),
                "proposal-fixture-audit" => await S.Audit(output, default),
                "proposal-fixture-publication-check" => S.PublicationCheck(output, true, default),
                "proposal-fixture-verify" => await S.Verify(output, File.ReadAllText(S.P(f.RegistryRoot, "closeout-pin.txt")).Trim(), default),
                _ => throw new InvalidDataException("Unknown maximum fixture command.")
            };
            Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
