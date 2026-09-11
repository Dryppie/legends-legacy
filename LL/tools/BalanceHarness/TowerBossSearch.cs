using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerBossSearch
{
    private delegate Task<(LoadoutTrial Trial, TowerBattleReport Report)> Evaluate(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token);

    private static IReadOnlyList<(string Enabler, string Consumer)> Pairs(TowerBossSearchDefinition d, TowerBossInventoryReport inventory)
    {
        var allowed = d.AllowedEssences.ToHashSet(StringComparer.Ordinal);
        var intents = d.Objective.StrategyIntent == "any"
            ? inventory.Bosses.Where(b => d.Objective.TargetFloorWeights.ContainsKey(b.FloorNumber)).SelectMany(b => b.CounterIntents).Distinct().ToArray()
            : [d.Objective.StrategyIntent];
        var relevant = inventory.Essences.Where(e => e.Signals.Any(s => intents.Contains(s.Replace("intent:", "", StringComparison.Ordinal))))
            .Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var all = inventory.EnablerConsumerPairs.Where(p => allowed.Contains(p.EnablerEssenceId) && allowed.Contains(p.ConsumerEssenceId))
            .Select(p => (Enabler: p.EnablerEssenceId, Consumer: p.ConsumerEssenceId)).Distinct().OrderBy(p => p.Enabler, StringComparer.Ordinal)
            .ThenBy(p => p.Consumer, StringComparer.Ordinal).ToArray();
        var focused = all.Where(p => relevant.Contains(p.Enabler) || relevant.Contains(p.Consumer)).ToArray();
        return focused.Length > 0 ? focused : all;
    }

    internal static BossBehavior Observe(IReadOnlyList<TowerBattleReport> reports, bool recovery = false)
    {
        // Aggregate existing observer fields only. No generated barrier, attempted heal, damage-taken or threat score.
        double Average(Func<TowerBattleReport, double> get) => reports.Count == 0 ? 0 : reports.Average(get);
        return new(Average(r => r.Battle.Summary.Telemetry.HostileSummonActiveTicks),
            Average(r => r.Battle.Summary.Telemetry.AverageInitialFriendlyHealthDeficitRatio),
            Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Friendly.Any(f => f.Id == s.EntityId))
                .Sum(s => (double)s.AvoidedDamage + s.TypedMitigationPrevented + s.BlockPrevented + s.DamageReductionPrevented + s.DamageBlocked)),
            // Regeneration is separately effective; direct/periodic/lifesteal attribution remains qualified in inventory.
            Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Friendly.Any(f => f.Id == s.EntityId)).Sum(s => (double)s.HealingDone)),
            Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Hostile.Any(h => h.Id == s.EntityId)).Sum(s => (double)s.ActionDeniedTicks)),
            recovery ? new(
                Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Friendly.Any(f => f.Id == s.EntityId)).Sum(s => (double)s.HealthRegenerated)),
                Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Hostile.Any(h => h.Id == s.EntityId)).Sum(s => (double)s.HealingDone)),
                Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Hostile.Any(h => h.Id == s.EntityId)).Sum(s => (double)s.HealthRegenerated))) : null);
    }

    private static async Task<TowerBossSearchReport> Execute(TowerBossSearchDefinition d,
        IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> contexts, TowerBossInventoryReport inventory,
        Evaluate evaluate, Action<string, object> snapshot, Action<string, BossProposal>? onProposal,
        CancellationToken token, Action<string>? progress, Action<TowerBossSearchReport>? checkpoint = null)
    {
        var arms = new List<BossSearchResult>(); var discovery = new List<BossMeasurement>();
        var selection = new List<PartyChoice>(); var confirmation = new List<BossMeasurement>(); var diagnostics = new List<BossMeasurement>();
        var strategies = new List<BossStrategy>(); var actual = 0;
        var planned = Validate(d);
        var aliases = Aliases(contexts, d.Controls[0]);
        var families = inventory.Essences.Where(e => d.AllowedEssences.Contains(e.Id)).ToDictionary(e => e.Id, e => e.SourceMonsterId);
        var pairs = Pairs(d, inventory);
        var refinementDiagnosis = d.Refinement is null ? null : TowerBossDiagnostics.Create(d, inventory);
        if (refinementDiagnosis is not null) snapshot("diagnostic-plan.json", refinementDiagnosis);
        var sameOwnerPairs = inventory.EnablerConsumerPairs.GroupBy(p => (Enabler: p.EnablerEssenceId, Consumer: p.ConsumerEssenceId))
            .Where(g => g.All(p => p.Compatibility == "same-owner-or-explicit-recipient-required")).Select(g => g.Key).ToHashSet();
        TowerBossSearchReport Report(string status) => new(status, planned, actual, 0, arms.ToArray(), discovery.ToArray(),
            selection.ToArray(), confirmation.ToArray(), strategies.ToArray(), aliases, diagnostics.ToArray());
        async Task<BossMeasurement> Measure(PartyChoice party, string arm, string stage,
            IReadOnlyDictionary<int, IReadOnlyList<int>> schedule, bool transfer, BossMeasurement? control)
        {
            var cells = new List<PartyFloorScore>(); var observations = new List<TowerBattleReport>();
            foreach (var floor in Enumerable.Range(1, 15).Where(f => transfer || d.Objective.TargetFloorWeights.ContainsKey(f)))
            {
                var measured = new Dictionary<string, PartyFloorScore>();
                foreach (var alias in aliases.Where(a => a.Floor == floor))
                {
                    if (alias.Context != alias.EvaluatedContext)
                    {
                        cells.Add(measured[alias.EvaluatedContext] with { Context = alias.Context });
                        continue;
                    }
                    var scenario = contexts[alias.Context].Single(s => s.FloorNumber == floor);
                    var recipe = TowerPartySelection.Apply(scenario, party.Builds, schedule[floor]);
                    var reports = new List<TowerBattleReport>(); var trials = new List<string>();
                    foreach (var seed in recipe.Seeds)
                    {
                        var result = await evaluate(arm + "@" + alias.Context, stage, recipe, seed, token);
                        actual++; reports.Add(result.Report); trials.Add(result.Trial.Id);
                        checkpoint?.Invoke(Report("Incomplete"));
                    }
                    var cell = TowerPartySelection.Cell(alias.Context, floor, reports, trials);
                    measured.Add(alias.Context, cell); cells.Add(cell);
                    if (d.Objective.TargetFloorWeights.ContainsKey(floor)) observations.AddRange(reports);
                }
            }
            var victories = observations.Where(r => r.Succeeded).ToArray();
            var duration = victories.Length == 0 ? double.MaxValue : victories.Average(r => r.Battle.Summary.DurationSeconds);
            return new(party.Id, TowerBossOptimization.Fitness(cells, control?.Cells ?? cells, d.Objective.TargetFloorWeights, duration), cells, Observe(observations, d.SchemaVersion == 2));
        }
        foreach (var generation in d.GenerationSeeds)
            foreach (var method in d.Methods)
            {
                var arm = method + "-" + generation.ToString(System.Globalization.CultureInfo.InvariantCulture);
                BossMeasurement? reference = null;
                var result = await TowerBossOptimization.RunAsync(method, generation, d.CandidatesPerArm, 10000,
                    d.Controls, families, d.MutablePartySlots, pairs, async (party, ct) =>
                    {
                        var row = await Measure(party, arm, "discovery", Schedule(d.DiscoverySeed, d.DiscoverySamples), false, reference);
                        reference ??= row;
                        discovery.Add(row);
                        checkpoint?.Invoke(Report("Incomplete"));
                        progress?.Invoke($"{arm}: discovery candidate {discovery.Count}; {actual} combats.");
                        return row;
                    }, token, p => onProposal?.Invoke(arm, p), sameOwnerPairs, d.Refinement?.AnchorId,
                    refinementDiagnosis?.Parties.Skip(1).ToArray());
                arms.Add(result); snapshot("searches/" + arm + ".json", result);
                if (result.Evaluations.Count != d.CandidatesPerArm)
                    throw new InvalidDataException("Proposal budget exhausted: comparison has unequal actual combat allocation and is incomplete.");
            }
        var distinct = discovery.DistinctBy(r => r.Id).ToArray();
        strategies.AddRange(TowerBossOptimization.Archive(distinct));
        var parties = arms.SelectMany(a => a.Parties).DistinctBy(p => p.Id).ToDictionary(p => p.Id);
        void Add(string id) { if (selection.All(p => p.Id != id)) selection.Add(parties[id]); }
        foreach (var control in d.Controls) Add(control.Id);
        foreach (var arm in arms) Add(TowerBossOptimization.Rank(arm.Evaluations).First().Id);
        foreach (var strategy in strategies) Add(strategy.Id);
        foreach (var row in TowerBossOptimization.Rank(distinct)) { if (selection.Count >= d.Finalists) break; Add(row.Id); }
        if (selection.Count > d.Finalists) throw new InvalidDataException("Frozen control/method/strategy allocation exceeds declared finalist cap.");
        var diagnosis = refinementDiagnosis ?? DiagnosticPlan(d, families, pairs, sameOwnerPairs);
        // These outputs are fixed before reading any fresh confirmation or diagnostic outcomes.
        snapshot("strategy-archive.json", strategies);
        snapshot("selection.json", selection);
        if (refinementDiagnosis is null) snapshot("diagnostic-plan.json", diagnosis);
        checkpoint?.Invoke(Report("Incomplete"));
        foreach (var party in selection)
        {
            confirmation.Add(await Measure(party, "confirmation-" + party.Id, "confirmation",
                Schedule(d.ConfirmationSeed, d.ConfirmationSamples), true, confirmation.FirstOrDefault()));
            snapshot("confirmation/" + party.Id + ".json", confirmation[^1]);
            checkpoint?.Invoke(Report("Incomplete"));
            progress?.Invoke($"Confirmed {confirmation.Count}/{selection.Count} frozen parties on 15 floors; {actual} combats.");
        }
        foreach (var party in diagnosis.Parties)
        {
            diagnostics.Add(await Measure(party, "diagnostic-" + party.Source, "diagnostic", Schedule(d.DiagnosticSeed, d.DiagnosticSamples), false, diagnostics.FirstOrDefault()));
            checkpoint?.Invoke(Report("Incomplete"));
            progress?.Invoke($"Reserved diagnostic {party.Source}; {actual} combats.");
        }
        snapshot("diagnostics.json", diagnostics);
        return Report("Complete");
    }

    private static BossDiagnosticPlan DiagnosticPlan(TowerBossSearchDefinition d, IReadOnlyDictionary<string, string> families,
        IReadOnlyList<(string Enabler, string Consumer)> pairs, IReadOnlySet<(string Enabler, string Consumer)> sameOwnerPairs)
    {
        var baseline = d.Controls[0];
        foreach (var pair in pairs)
        {
            if (pair.Enabler == pair.Consumer || baseline.Builds.Values.Any(ids => ids.Contains(pair.Enabler) || ids.Contains(pair.Consumer))) continue;
            foreach (var first in d.MutablePartySlots)
                foreach (var second in d.MutablePartySlots.Where(s => sameOwnerPairs.Contains(pair) ? s == first : s != first))
                {
                    var consumerPosition = first == second ? 1 : 0;
                    if (baseline.Builds[first].Skip(1).Any(id => families[id] == families[pair.Enabler])
                        || baseline.Builds[second].Where((_, i) => i != consumerPosition).Any(id => families[id] == families[pair.Consumer])
                        || (first == second && families[pair.Enabler] == families[pair.Consumer])) continue;
                    PartyChoice Replacement(string source, bool enabler, bool consumer)
                    {
                        var builds = baseline.Builds.ToDictionary(b => b.Key, b => b.Value);
                        if (enabler) builds[first] = new[] { pair.Enabler }.Concat(builds[first].Skip(1)).ToArray();
                        if (consumer) { var ids = builds[second].ToArray(); ids[consumerPosition] = pair.Consumer; builds[second] = ids; }
                        return TowerPartySelection.Choice(source, builds);
                    }
                    return new("planned", pair.Enabler, pair.Consumer,
                        [Replacement("baseline", false, false), Replacement("enabler-alone", true, false), Replacement("consumer-alone", false, true), Replacement("combination", true, true)],
                        "First legal graph pair absent from the reference party, selected without outcomes. Same-owner restrictions preserve same-character placement. Matched replacements on reserved seeds support a local interaction hypothesis only; no death-prevention or universal synergy claim.");
                }
        }
        return new("unsupported", null, null, [], "No eligible absent enabler/consumer pair with two legal teammate replacements; no diagnostic interaction claim. Reserved allowance remains unused.");
    }

    public static async Task<TowerBossSearchReport> RunAsync(string root, string catalogs, string output,
        TowerBossSearchDefinition d, CancellationToken token = default, Action<string>? progress = null)
    {
        var planned = Validate(d);
        if (Path.Exists(output)) throw new IOException("Choose a new boss-search output directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "searches", "confirmation", "catalogs" }) Directory.CreateDirectory(Path.Combine(output, folder));
        TowerLoadoutArchive? archive = null; TowerBossSearchReport? report = null; var status = "Invalid";
        try
        {
            var settings = TowerBundle.ReadSettings(root); var frozen = Path.Combine(output, "content");
            var scope = new LoadoutScope(Algorithm(d), settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(root, frozen, token), "gzip-json-v1");
            HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope);
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), d);
            File.Copy(Path.Combine(catalogs, "tower-curve.json"), Path.Combine(output, "catalogs/tower-curve.json"));
            if (d.ValidationReferenceHash is not null)
            {
                ValidateValidationReferences(catalogs, d);
                File.Copy(Path.Combine(catalogs, TowerBossValidationReferences.FileName),
                    Path.Combine(output, "catalogs", TowerBossValidationReferences.FileName));
                ValidateValidationReferences(Path.Combine(output, "catalogs"), d);
            }
            if (d.Refinement?.ReferenceSetId is not null)
            {
                File.Copy(TowerBossReferences.CatalogPath(catalogs), Path.Combine(output, "catalogs/tower-boss-references.json"));
                ValidateReferences(frozen, Path.Combine(output, "catalogs"), d, scope);
            }
            var inventory = TowerBossInventory.Create(frozen, settings.Threat);
            HarnessJson.WriteNew(Path.Combine(output, "boss-profiles.json"), inventory);
            File.WriteAllText(Path.Combine(output, "boss-profiles.md"), TowerBossInventory.Markdown(inventory));
            var contexts = Contexts(frozen, Path.Combine(output, "catalogs"), d);
            HarnessJson.WriteNew(Path.Combine(output, "contexts.json"), contexts);
            HarnessJson.WriteNew(Path.Combine(output, "seed-ledger.json"), Ledger(d));
            await ValidateContent(frozen, d, scope, contexts, inventory, token);
            archive = new(output, scope, d.MaximumBattles);
            report = await Execute(d, contexts, inventory, archive.EvaluateAsync,
                (path, value) => HarnessJson.WriteNew(Path.Combine(output, path), value),
                (arm, proposal) => File.AppendAllText(Path.Combine(output, "searches", arm + ".jsonl"),
                    JsonSerializer.Serialize(proposal, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n"),
                token, progress, partial => report = partial);
            if (archive.Trials.Count != report.ActualBattles || archive.CacheHits != 0)
                throw new InvalidDataException("Actual combat ledger differs from declared evaluation accounting.");
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        catch (Exception error) { HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { error.Message, Type = error.GetType().Name }); throw; }
        finally
        {
            report ??= new(status, planned, archive?.Trials.Count ?? 0, archive?.CacheHits ?? 0, [], [], [], [], [], [], []);
            report = report with { Status = status, ActualBattles = archive?.Trials.Count ?? 0, CacheHits = archive?.CacheHits ?? 0 };
            HarnessJson.WriteNew(Path.Combine(output, "boss-search.json"), report);
            var diagnosticPath = Path.Combine(output, "diagnostic-plan.json");
            File.WriteAllText(Path.Combine(output, "boss-search.md"), Markdown(report, d,
                File.Exists(diagnosticPath) ? HarnessJson.Read<BossDiagnosticPlan>(diagnosticPath) : null));
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));
        }
        return report;
    }

    private static object Ledger(TowerBossSearchDefinition d) => new {
        Discovery = Schedule(d.DiscoverySeed, d.DiscoverySamples).Where(p => d.Objective.TargetFloorWeights.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value),
        Confirmation = Schedule(d.ConfirmationSeed, d.ConfirmationSamples),
        Diagnostic = Schedule(d.DiagnosticSeed, d.DiagnosticSamples).Where(p => d.Objective.TargetFloorWeights.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value),
        d.ExcludedCombatSeeds, Note = "Schedules freeze before discovery. All methods/restarts share paired combat seeds, not independent extra observations. Confirmation never reselects recipes or categories." };

    private static async Task ValidateContent(string root, TowerBossSearchDefinition d, LoadoutScope scope,
        IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> contexts, TowerBossInventoryReport inventory, CancellationToken token)
    {
        var families = inventory.Essences.ToDictionary(e => e.Id, e => e.SourceMonsterId);
        if (d.AllowedEssences.Except(families.Keys).Any() || d.Controls.Any(c => c.Builds.Values.Any(ids =>
            ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)))
            throw new InvalidDataException("Unknown allowed Essence or duplicate source family in a control.");
        var validator = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        foreach (var control in d.Controls)
            foreach (var scenario in contexts.Values.SelectMany(s => s))
            {
                var recipe = TowerPartySelection.Apply(scenario, control.Builds, [1]);
                await validator.PrepareAsync(validator.CreateInput(recipe, 1, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
            }
    }

    private static void ValidateReferences(string root, string catalogs, TowerBossSearchDefinition d, LoadoutScope scope)
    {
        var references = TowerBossReferences.Load(root, catalogs, d.Budget.PriorityFloor, d.Budget.EssenceSlots, scope.Settings, scope.Execution)
            ?? throw new InvalidDataException("The declared portable boss reference set is missing.");
        if (references.ReferenceSetId != d.Refinement?.ReferenceSetId
            || references.AnchorId != d.Refinement.AnchorId || references.EvidenceStatus != d.Refinement.ReferenceEvidenceStatus
            || references.Controls.Any(c => !d.Controls.Any(p => HarnessJson.Hash(p) == HarnessJson.Hash(c)))
            || references.ExcludedCombatSeeds.Except(d.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("The declared historical boss references or seed exclusions were changed or omitted.");
    }

    private static void ValidateValidationReferences(string catalogs, TowerBossSearchDefinition d)
    {
        if (d.ValidationReferenceHash is null) return;
        var path = Path.Combine(catalogs, TowerBossValidationReferences.FileName);
        if (!File.Exists(path) || HarnessJson.FileHash(path) != d.ValidationReferenceHash)
            throw new InvalidDataException("The declared fixed validation evidence is missing or changed.");
        var validation = TowerBossValidationReferences.Read(catalogs)!;
        if (validation.ExcludedCombatSeeds.Except(d.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Fixed validation seed exclusions were omitted from this boss experiment.");
    }

    public static async Task<TowerBossSearchReport> VerifyAsync(string output, CancellationToken token = default)
        => await VerifyAsync(output, TowerLoadoutArchive.Verify(output, token), null, token);

    // The dashboard already verifies the complete inventory on every request. Keep its ledger and
    // compact battle previews from this same reconstruction, rather than reading the archive twice.
    internal static async Task<TowerBossSearchReport> VerifyAsync(string output, IReadOnlyList<LoadoutTrial> trials,
        Action<LoadoutTrial, TowerBattleReport>? observed, CancellationToken token)
    {
        var d = Read(Path.Combine(output, "definition.json"));
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        var report = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(output, "boss-search.json"));
        if (report.Status != "Complete" || report.ActualBattles != trials.Count || report.PlannedMaximum != Validate(d)
            || scope.Algorithm != Algorithm(d) || report.CacheHits != 0) throw new InvalidDataException("Incomplete or incompatible boss search.");
        void Snapshot(string path, object value)
        {
            var saved = HarnessJson.Read<JsonElement>(Path.Combine(output, path));
            if (HarnessJson.Hash(saved) != HarnessJson.Hash(value)) throw new InvalidDataException("Reconstructed boss search differs: " + path);
        }
        var root = Path.Combine(output, "content");
        foreach (var content in scope.ContentHashes)
            if (HarnessJson.FileHash(Path.Combine(root, "Data", content.Key)) != content.Value) throw new InvalidDataException("Frozen boss search content changed.");
        ValidateValidationReferences(Path.Combine(output, "catalogs"), d);
        if (d.Refinement?.ReferenceSetId is not null) ValidateReferences(root, Path.Combine(output, "catalogs"), d, scope);
        var inventory = TowerBossInventory.Create(root, scope.Settings.Threat);
        Snapshot("boss-profiles.json", inventory);
        if (File.ReadAllText(Path.Combine(output, "boss-profiles.md")) != TowerBossInventory.Markdown(inventory)) throw new InvalidDataException("Boss profile Markdown changed.");
        var contexts = Contexts(root, Path.Combine(output, "catalogs"), d);
        Snapshot("contexts.json", contexts); Snapshot("seed-ledger.json", Ledger(d));
        await ValidateContent(root, d, scope, contexts, inventory, token);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var index = 0; var proposals = new Dictionary<string, List<BossProposal>>();
        TowerScenario? previousScenario = null;
        TowerBattleInput? preparedInput = null;
        string? recipeIdentity = null;
        var rebuilt = await Execute(d, contexts, inventory, (arm, stage, scenario, seed, ct) => {
            ct.ThrowIfCancellationRequested();
            if (index >= trials.Count) throw new InvalidDataException("Missing recorded boss combat.");
            var trial = trials[index++];
            // Measure sends the exact same scenario instance for consecutive seeds. Its party,
            // content and settings are seed independent; only Rules.RandomSeed changes. Retain
            // one template, still reconstructing and checking the full input/cache hash per trial.
            if (!ReferenceEquals(previousScenario, scenario))
            {
                preparedInput = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                recipeIdentity = HarnessJson.Hash(scenario);
                var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", recipeIdentity + ".json"));
                if (HarnessJson.Hash(recipe) != recipeIdentity) throw new InvalidDataException("Saved boss export recipe changed.");
                previousScenario = scenario;
            }
            if (!scenario.Seeds.Contains(seed)) throw new InvalidDataException("Undeclared boss combat seed.");
            var input = preparedInput! with { Rules = preparedInput!.Rules with { RandomSeed = seed } };
            if (trial.Stage != stage || trial.Seed != seed || trial.Recipe != recipeIdentity
                || trial.InputHash != HarnessJson.Hash(input) || trial.CacheKey != TowerLoadoutArchive.Key(scope, arm, input))
                throw new InvalidDataException("Recorded combat differs from deterministic boss recipe, arm or stage schedule.");
            var battle = TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage);
            if (battle.Battle.Seed != seed || battle.Battle.ScenarioId != scenario.Id) throw new InvalidDataException("Recorded boss battle identity changed.");
            observed?.Invoke(trial, battle);
            return Task.FromResult((trial, battle));
        }, Snapshot, (arm, proposal) => { if (!proposals.TryGetValue(arm, out var list)) proposals.Add(arm, list = []); list.Add(proposal); }, token, null);
        foreach (var arm in proposals)
        {
            var saved = File.ReadLines(Path.Combine(output, "searches", arm.Key + ".jsonl")).Select(s => JsonSerializer.Deserialize<BossProposal>(s, HarnessJson.Options)).ToArray();
            if (HarnessJson.Hash(saved) != HarnessJson.Hash(arm.Value)) throw new InvalidDataException("Boss candidate provenance changed.");
        }
        if (index != trials.Count || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(report)) throw new InvalidDataException("Boss report differs from archived combat reconstruction.");
        if (File.ReadAllText(Path.Combine(output, "boss-search.md")) != Markdown(rebuilt, d,
            HarnessJson.Read<BossDiagnosticPlan>(Path.Combine(output, "diagnostic-plan.json")))) throw new InvalidDataException("Boss report Markdown differs.");
        return rebuilt;
    }
}
