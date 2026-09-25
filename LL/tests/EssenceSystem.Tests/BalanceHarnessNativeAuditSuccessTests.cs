using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

// Fabricated outcomes with real content and input identities. Never uses RunOwned,
// Inspect, Reserve, PrepareAsync, a combat evaluator, or production randomness.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessNativeAuditSuccessTests
{
    private static readonly JsonSerializerOptions Lines = new(HarnessJson.Options) { WriteIndented = false };

    private static TowerProposalContext Context(string content, TowerSettings settings)
    {
        var p = BalanceHarnessLoadoutPlacementTests.Plan().Racing;
        var inventory = TowerBossInventory.Create(content, settings.Threat);
        var pool = inventory.Essences.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
        var ids = p.Scope.AllowedEssences.Select((e, i) => (e.Id, Actual: pool[i].Id)).ToDictionary(p => p.Id, p => p.Actual);
        var scope = p.Scope with {
            ContentHashes = TowerCompactBundle.ContentHashes(content, default),
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()),
            AllowedEssences = pool.Select(e => p.Scope.AllowedEssences[0] with { Id = e.Id, Family = e.SourceMonsterId }).ToArray(),
            Starts = p.Scope.Starts.Select(s => s with { Party = TowerPartySelection.Choice("literal-native-audit", s.Party.Builds
                .ToDictionary(b => b.Key, b => (IReadOnlyList<string>)b.Value.Select(id => ids[id]).Order(StringComparer.Ordinal).ToArray())) }).ToArray()
        };
        scope = scope with { References = scope.References.Select(r => r with {
            Scenario = TowerBossDiscovery.Scenario(scope, r.Context, scope.Starts.Single(s => s.ReferenceId == r.Id).Party, [])
        }).ToArray() };
        var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(scope), inventory);
        return new(scope, mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory);
    }

    private sealed class LiteralArchive
    {
        private readonly string root;
        private readonly LoadoutScope scope;
        private readonly TowerBattleRunner runner;
        private readonly bool search;
        public LiteralArchive(string root, string algorithm, string content, TowerSettings settings, bool search)
        {
            this.root = root; this.search = search;
            Directory.CreateDirectory(Path.Combine(root, "recipes"));
            Directory.CreateDirectory(Path.Combine(root, "battles"));
            var hashes = TowerBundle.CopyContent(content, Path.Combine(root, "content"), default);
            scope = new(algorithm, settings, ExecutionIdentity.Current(), hashes, "gzip-json-v1");
            HarnessJson.WriteNew(Path.Combine(root, "scope.json"), scope);
            runner = new(Path.Combine(root, "content"), new OfflineContent(Path.Combine(root, "content"), settings.Threat));
        }
        public (LoadoutTrial Trial, int MaximumTicks) Bind(TowerPanelTrial request)
        {
            var input = runner.CreateInput(request.Scenario, request.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
            var arm = search ? TowerProposalRacingNative.Arm(scope.Algorithm, request) : scope.Algorithm;
            return (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
                HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, arm, input)), input.Rules.MaxTicks);
        }
        public (LoadoutTrial Trial, TowerBattleReport Report) Write(TowerPanelTrial request, LoadoutTrial trial)
        {
            // All draws keep the fixture deterministic and make no efficacy claim.
            var report = new TowerBattleReport(new BattleReport(1, request.Scenario.Id, request.Seed,
                FastCombatEngine.TicksPerSecond, JsonSerializer.SerializeToElement(new { literalNativeAuditFixture = true }),
                new BattleSummary(BattleOutcome.Draw, BattleOutcome.Draw, "Literal audit fixture", FastCombatEngine.TicksPerSecond, 1,
                    [new SimpleCombatEntity("fixture", "fixture", "", 10, 0)], [], [], new CompactCombatTelemetry()), null), false, 0, 1);
            var recipe = Path.Combine(root, "recipes", trial.Recipe + ".json");
            if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, request.Scenario);
            TowerLoadoutArchive.WriteBattle(root, trial.Id, report, "gzip-json-v1");
            File.AppendAllText(Path.Combine(root, "trials.jsonl"), JsonSerializer.Serialize(trial, Lines) + "\n");
            return (trial, report);
        }
        public void Seal() => S.Seal(root, default);
    }

    [Fact]
    public async Task Full_compressed_fixture_passes_production_audit_with_materialized_inputs_and_literal_outcomes()
    {
        var export = Environment.GetEnvironmentVariable("LL_NATIVE_AUDIT_SUCCESS_EXPORT");
        var root = Path.GetFullPath(export ?? Path.Combine(Path.GetTempPath(), "tower-native-audit-fixture-" + Guid.NewGuid().ToString("N")));
        Assert.False(Path.Exists(root));
        Directory.CreateDirectory(root);
        try
        {
            using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal audit fixture entered combat.")).Activate();
            var package = Path.Combine(root, "package"); Directory.CreateDirectory(package);
            var registry = Path.Combine(root, "literal-registry"); Directory.CreateDirectory(registry);
            var output = Path.Combine(registry, "result"); Directory.CreateDirectory(output);
            var content = Path.Combine(package, "content");
            TowerBundle.CopyContent(TestContentPaths.FindApiRoot(), content, default);
            var settings = new TowerSettings(new(), 10);
            var context = Context(content, settings);
            var history = S.LegacySeeds(context).Concat(context.Scope.Generation.Seeds).Append(context.RootSeed)
                .Concat(context.Scope.References.SelectMany(r => r.Scenario.Seeds)).Distinct().Order().ToArray();
            context = context with { Scope = context.Scope with { ExcludedCombatSeeds = history.Except(S.LegacySeeds(context)).ToArray() } };
            var affinities = TowerDamageSourceAffinities.Create(context.DamageAffinityInventory!).Affinities;
            var policy = TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(affinities.Select(a => a.Id));
            var plan = TowerProposalComparison.CreateLoadoutPlacementPlan(context, policy);
            var repo = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../.."));
            foreach (var module in TowerProposalEvidenceStorage.Modules)
                File.Copy(Path.Combine(repo, "Balance Harness/analysis", module), Path.Combine(output, module));
            var storageContract = new ProposalEvidenceStorage(TowerProposalEvidenceCodec.Version, 1024L * 1048576,
                1024L * 1048576, 72, HarnessJson.FileHash(Path.Combine(output, TowerProposalEvidenceStorage.Modules[0])),
                HarnessJson.FileHash(Path.Combine(output, TowerProposalEvidenceStorage.Modules[1])));
            var inputs = new ProposalStudyInputs(plan, context, settings, history, new(new Dictionary<string, string>(), history), storageContract);
            var runtime = TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current(), 512L * 1048576, default);
            foreach (var (name, value) in new (string, object)[] { ("plan", plan), ("context", context), ("settings", settings), ("history", history), ("runtime", runtime) })
            {
                HarnessJson.WriteNew(Path.Combine(package, name + ".json"), value);
                Directory.CreateDirectory(Path.Combine(output, "source"));
                File.Copy(Path.Combine(package, name + ".json"), Path.Combine(output, "source", name + ".json"));
            }
            ProposalStudyFile Source(string name) => new(Path.Combine(package, name + ".json"), HarnessJson.FileHash(Path.Combine(package, name + ".json")));
            // Request bindings are structurally real; no admission/launch receipt is created.
            var request = new ProposalStudyRequest(plan.Version, Source("plan"), Source("context"), Source("settings"), Source("history"), Source("runtime"),
                Source("plan"), content, registry, output, new Dictionary<string, string> { [Source("history").Path] = Source("history").Sha256 },
                new Dictionary<string, string>(), new Dictionary<string, string>(), S.ResourceV2, storageContract);
            HarnessJson.WriteNew(Path.Combine(output, "request.json"), request);
            TowerBundle.CopyContent(content, Path.Combine(output, "content"), default);
            S.ValidateContent(inputs, Path.Combine(output, "content"), default);
            // Literal bytes only. Do not call Reserve or consult the scientific registry.
            var bytes = new byte[S.EntropyWords * 4];
            for (var i = 0; i < S.EntropyWords; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), 200000 + i);
            var allocation = S.Classify(bytes, history, plan.Version);
            File.WriteAllBytes(Path.Combine(output, "entropy.bin"), bytes);
            HarnessJson.WriteNew(Path.Combine(output, "allocation.json"), allocation);
            HarnessJson.WriteNew(Path.Combine(output, "entropy-intent.json"), new { version = plan.Version, words = S.EntropyWords,
                assignedValues = plan.RequiredFreshValues, historicalHash = HarnessJson.Hash(history), retries = 0 });
            HarnessJson.WriteNew(Path.Combine(output, "history-input.json"), new { reservationState = "Complete", reserved = allocation.Reserved });
            HarnessJson.WriteNew(Path.Combine(output, "seed-ledger.json"), new { reservationState = "Complete", historical = history, reserved = allocation.Reserved });
            var storage = new TowerProposalEvidenceStorage.Writer(output, storageContract, () => { });
            Directory.CreateDirectory(Path.Combine(output, "study"));
            using var attempts = new TowerPracticalSearch.Attempts(Path.Combine(output, "attempts.jsonl"), plan.MaximumFights, () => { });
            LiteralArchive? heldout = null;
            var completed = 0;
            var result = await S.Execute(plan, context, allocation.Selected, pair => TowerProposalComparison.ExecutePairAsync(plan, pair, (_, _) => { }, async (arm, p) => {
                var folder = S.SearchRoot(output, pair.Root, arm);
                var archive = new LiteralArchive(folder, TowerProposalRacingNative.ArchiveAlgorithm(p), content, settings, true);
                TowerPanelTrial current = null!; (LoadoutTrial Trial, int MaximumTicks) binding = default;
                var report = await TowerProposalRacingNative.ExecuteAsync(p, Path.Combine(folder, "racing"), 256L * 1048576,
                    r => { current = r; binding = archive.Bind(r); return binding; },
                    (_, _, _, _, _) => Task.FromResult(archive.Write(current, binding.Trial)), () => { }, default, attempts.Event, storage);
                Assert.Equal("Complete", report.Evaluation.Status); archive.Seal(); completed++; return report;
            }), (freeze, r) => {
                Assert.Equal(24, completed);
                heldout ??= new(Path.Combine(output, "heldout"), plan.Version + "/heldout/" + HarnessJson.Hash(freeze), content, settings, false);
                var binding = heldout.Bind(r); var row = heldout.Write(r, binding.Trial);
                return Task.FromResult(TowerAdaptiveRacingNative.Authenticate(r, binding.Trial, binding.MaximumTicks, row.Trial, row.Report));
            }, (name, value) => {
                if (TowerProposalEvidenceStorage.Eligible(name)) storage.Put(Path.Combine(output, "study"), name, value);
                else HarnessJson.WriteNew(Path.Combine(output, "study", name), value);
            }, () => S.AttemptPrefix(Path.Combine(output, "attempts.jsonl")), attempts.Event, default);
            attempts.Dispose(); heldout!.Seal();
            var freeze = HarnessJson.Read<ProposalStudyFreeze>(Path.Combine(output, "study/freeze.json"));
            storage.SealDirectory(Path.Combine(output, "study"), Enumerable.Range(1, 12).Select(n => $"pair-{n:D2}.json")
                .Concat(freeze.Families.SelectMany(f => f.Members.Select(m => $"heldout-{f.Root:D2}-{m.RecipeHash}.json"))).ToArray(),
                (name, value) => HarnessJson.WriteNew(Path.Combine(output, "study", name), value));
            storage.Finish(); S.Seal(Path.Combine(output, "study"), default);
            HarnessJson.WriteNew(Path.Combine(output, "provisional-result.json"), result);
            var work = new TowerWorkAccounting();
            using (work.Activate()) Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await S.Audit(output, default)));
            Assert.Equal(12, work.Snapshot()["reconstructedRoots"]);
            Assert.Equal(24, work.Snapshot()["reconstructedTrajectories"]);
            Assert.Equal(12, work.Snapshot()["reconstructedCatalogues"]);
            Assert.Equal(result.Fights, work.Snapshot()["reconstructedTrialBindings"]);
            Assert.Equal(1, work.Snapshot()["reconstructedStudyEndpoints"]);
            HarnessJson.WriteNew(Path.Combine(root, "backend-audit-counters.json"), work.Snapshot());
            HarnessJson.WriteNew(Path.Combine(root, "fixture.json"), new {
                version = "tower-native-audit-success-fixture-v1", fixtureOnly = true, literalOutcomes = true,
                actualCombat = 0, productionEntropyDraws = 0, scientificReservations = 0, nativeEncounterPreparations = 0,
                productionAuditPassed = true, scientificAdmitted = false, usableForAdmission = false,
                outputRoot = output, nativeDll = Path.Combine(output, "executable/BalanceHarness.dll"),
                requestSha256 = HarnessJson.FileHash(Path.Combine(output, "request.json")), result.Fights, result.HeldoutFights,
                expectedRoots = 12, expectedTrajectories = 24, expectedCatalogues = 12, expectedEndpoints = 1,
                expectedHeldoutMembers = freeze.Families.Sum(f => f.Members.Count)
            });
            S.Seal(root, default);
        }
        finally { if (export is null) Directory.Delete(root, true); }
    }
}
