using System.Globalization;
using System.Text;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record BossDiscoveryRunReport(string Status, int PlannedDiscoveryBattles, int ActualBattles, int CacheHits,
    BossGenerationResult? Generation, string? Error);

/// <summary>Discovery only, using the normal Tower archive. Selection validation and confirmation are separate later stages.</summary>
public static class TowerBossDiscoveryRun
{
    public const string Algorithm = TowerBossDiscovery.Version + "/" + TowerBossGeneration.Version + "/discovery-only";
    internal delegate Task<(LoadoutTrial Trial, TowerBattleReport Report)> Battle(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token);

    internal static async Task<BossDiscoveryMeasurement> Measure(TowerBossDiscoveryDefinition d, BossDiscoveryInputs inputs,
        PartyChoice party, string arm, Battle battle, CancellationToken token, string stage = "discovery")
    {
        TowerBossDiscovery.ValidateParty(d, party);
        var cells = new List<PartyFloorScore>(); var all = new List<TowerBattleReport>();
        foreach (var context in inputs.DiscoverySeeds.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var scenario = TowerBossDiscovery.Scenario(d, context.Key, party, context.Value);
            var reports = new List<TowerBattleReport>(); var trials = new List<string>();
            foreach (var seed in scenario.Seeds)
            {
                var result = await battle(arm + "@" + context.Key, stage, scenario, seed, token);
                if (result.Report.Battle.Seed != seed || result.Report.Battle.ScenarioId != scenario.Id
                    || !Enum.IsDefined(result.Report.Battle.Summary.ContentOutcome)
                    || result.Report.Succeeded != (result.Report.Battle.Summary.ContentOutcome == BattleOutcome.Victory))
                    throw new InvalidDataException("Discovery battle outcome or identity is invalid.");
                reports.Add(result.Report); trials.Add(result.Trial.Id);
            }
            cells.Add(TowerPartySelection.Cell(context.Key, d.Budget.PriorityFloor, reports, trials)); all.AddRange(reports);
        }
        var wins = all.Where(r => r.Succeeded).ToArray();
        return new(party.Id, TowerBossGeneration.Fitness(inputs, cells, wins.Length == 0 ? double.MaxValue : wins.Average(r => r.Battle.Summary.DurationSeconds)),
            cells, TowerBossSearch.Observe(all, recovery: true));
    }

    public static async Task<BossDiscoveryRunReport> RunAsync(string root, string output, TowerBossDiscoveryDefinition definition,
        CancellationToken token = default, Action<string>? progress = null)
    {
        var cost = TowerBossDiscovery.Validate(root, definition);
        var inputs = TowerBossImprovement.Inputs(definition);
        if (Path.Exists(output)) throw new IOException("Choose a new independent discovery output directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "shortlist" }) Directory.CreateDirectory(Path.Combine(output, folder));
        TowerLoadoutArchive? archive = null; BossGenerationResult? generation = null;
        var status = "Invalid"; string? error = null;
        BossDiscoveryRunReport Report() => new(status, cost.Discovery, archive?.Trials.Count ?? 0, archive?.CacheHits ?? 0, generation, error);
        try
        {
            var settings = TowerBundle.ReadSettings(root);
            var frozen = Path.Combine(output, "content");
            var scope = new LoadoutScope(TowerBossImprovement.Algorithm(definition) + "/discovery-only", settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(root, frozen, token), "gzip-json-v1");
            if (HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(definition.ContentHashes)
                || HarnessJson.Hash(scope.Settings) != definition.SettingsHash || HarnessJson.Hash(scope.Execution) != definition.ExecutionHash)
                throw new InvalidDataException("Content/settings/execution changed while freezing discovery.");
            HarnessJson.WriteNew(Path.Combine(output, "scope.json"), scope);
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
            HarnessJson.WriteNew(Path.Combine(output, "cost.json"), cost);
            HarnessJson.WriteNew(Path.Combine(output, "generation-inputs.json"), inputs);
            if (definition.Mode == TowerBossDiscovery.Improve) HarnessJson.WriteNew(Path.Combine(output, "improvement-starts.json"), definition.Starts);
            var inventory = TowerBossInventory.Create(frozen, settings.Threat);
            var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
            HarnessJson.WriteNew(Path.Combine(output, "boss-profiles.json"), inventory);
            HarnessJson.WriteNew(Path.Combine(output, "generation-mechanics.json"), mechanics);
            archive = new(output, scope, cost.Discovery);
            var lastCount = -1;
            generation = await TowerBossImprovement.ExecuteAsync(definition, inputs, mechanics,
                (party, arm, ct) => Measure(definition, inputs, party, arm, archive.EvaluateAsync, ct), token, partial => {
                    generation = partial;
                    var count = partial.Arms.Sum(a => a.Evaluations.Count);
                    if (count != lastCount) { lastCount = count; progress?.Invoke($"Independent discovery: {count} evaluated parties; {archive.Trials.Count}/{cost.Discovery} combats."); }
                });
            status = generation.Status; error = generation.Error;
            TowerBossDiscovery.ValidateProvenance(definition, generation.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
            var measuredCost = generation.Arms.Sum(a => a.Evaluations.Count) * inputs.DiscoverySeeds.Values.Sum(s => s.Count);
            if (status is "Complete" or "Incomplete" && (archive.Trials.Count != measuredCost || archive.CacheHits != 0
                || status == "Complete" && archive.Trials.Count != cost.Discovery))
                throw new InvalidDataException("Discovery accounting differs from its actual trial ledger.");
            foreach (var party in generation.DiscoveryShortlist)
            foreach (var context in definition.Contexts)
            {
                // Export the already-measured discovery recipe, not an unused-seed confirmation claim.
                var scenario = TowerBossDiscovery.Scenario(definition, context.Id, party, inputs.DiscoverySeeds[context.Id]);
                HarnessJson.WriteNew(Path.Combine(output, "shortlist", party.Id + "-" + context.Id + ".json"), scenario);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        finally
        {
            var report = Report();
            HarnessJson.WriteNew(Path.Combine(output, "discovery.json"), report);
            File.WriteAllText(Path.Combine(output, "discovery.md"), Markdown(report));
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));
        }
        return Report();
    }

    public static async Task<BossDiscoveryRunReport> VerifyAsync(string output, CancellationToken token = default)
    {
        if (File.Exists(Path.Combine(output, TowerBulkCampaign.ContractFile))) return await TowerCompactDiscovery.VerifyAsync(output, token);
        var trials = TowerLoadoutArchive.Verify(output, token);
        var saved = HarnessJson.Read<BossDiscoveryRunReport>(Path.Combine(output, "discovery.json"));
        if (saved.Status is not ("Complete" or "Incomplete")) throw new InvalidDataException("Only complete or attempt-exhausted discovery supports full reconstruction; interrupted evidence remains partial.");
        var d = TowerBossDiscovery.Read(Path.Combine(output, "definition.json"));
        var cost = TowerBossDiscovery.Validate(d); var inputs = TowerBossImprovement.Inputs(d);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        if (scope.Algorithm != TowerBossImprovement.Algorithm(d) + "/discovery-only" || HarnessJson.Hash(scope.Execution) != d.ExecutionHash
            || d.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current()) || HarnessJson.Hash(scope.Settings) != d.SettingsHash
            || HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(d.ContentHashes)
            || trials.Count > cost.Discovery || saved.CacheHits != 0)
            throw new InvalidDataException("Discovery scope or producing execution changed; use the retained executable.");
        void Match<T>(string name, T value)
        {
            if (HarnessJson.Hash(HarnessJson.Read<T>(Path.Combine(output, name))) != HarnessJson.Hash(value))
                throw new InvalidDataException("Discovery artifact differs from reconstruction: " + name);
        }
        var root = Path.Combine(output, "content");
        if (d.ContentHashes.Any(p => HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) != p.Value))
            throw new InvalidDataException("Discovery frozen content changed.");
        Match("cost.json", cost); Match("generation-inputs.json", inputs);
        if (d.Mode == TowerBossDiscovery.Improve) Match("improvement-starts.json", d.Starts);
        var inventory = TowerBossInventory.Create(root, scope.Settings.Threat);
        var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
        Match("boss-profiles.json", inventory); Match("generation-mechanics.json", mechanics);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var index = 0;
        TowerScenario? lastScenario = null; TowerBattleInput? template = null; string? recipeHash = null;
        var result = await TowerBossImprovement.ExecuteAsync(d, inputs, mechanics, (party, arm, ct) => Measure(d, inputs, party, arm,
            (trialArm, stage, scenario, seed, ct2) => {
                ct2.ThrowIfCancellationRequested();
                if (index >= trials.Count) throw new InvalidDataException("Missing discovery trial.");
                if (!ReferenceEquals(lastScenario, scenario))
                {
                    template = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                    recipeHash = HarnessJson.Hash(scenario); lastScenario = scenario;
                    Match("recipes/" + recipeHash + ".json", scenario);
                }
                var input = template! with { Rules = template!.Rules with { RandomSeed = seed } };
                var trial = trials[index++];
                if (trial.Stage != stage || trial.Seed != seed || trial.Recipe != recipeHash
                    || trial.InputHash != HarnessJson.Hash(input) || trial.CacheKey != TowerLoadoutArchive.Key(scope, trialArm, input))
                    throw new InvalidDataException("Discovery trial recipe, identity, stage, arm or seed differs.");
                return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage)));
            }, ct), token);
        token.ThrowIfCancellationRequested();
        TowerBossDiscovery.ValidateProvenance(d, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var rebuilt = new BossDiscoveryRunReport(result.Status, cost.Discovery, trials.Count, 0, result, result.Error);
        if (index != trials.Count || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(saved))
            throw new InvalidDataException("Discovery proposals, evaluations, shortlist or accounting differ from deterministic reconstruction.");
        foreach (var party in result.DiscoveryShortlist)
        foreach (var context in d.Contexts)
            Match("shortlist/" + party.Id + "-" + context.Id + ".json", TowerBossDiscovery.Scenario(d, context.Id, party, inputs.DiscoverySeeds[context.Id]));
        if (File.ReadAllText(Path.Combine(output, "discovery.md")) != Markdown(rebuilt)) throw new InvalidDataException("Discovery Markdown differs.");
        return rebuilt;
    }

    public static string Markdown(BossDiscoveryRunReport report)
    {
        var text = new StringBuilder($"# Independent team discovery: {report.Status}\n\n");
        text.AppendLine($"Actual combats: {report.ActualBattles}/{report.PlannedDiscoveryBattles}; cache hits: {report.CacheHits}.\n");
        text.AppendLine("These are discovery results on reused discovery seeds, not confirmation or Tower balance acceptance. Stronger-than-50% parties remain visible. The shortlist requires separate selection validation and fresh confirmation. No benchmark reference was used as a parent or scored by this pass.\n");
        if (report.Error is not null) text.AppendLine(report.Error + "\n");
        text.AppendLine("| Method | Generation seed | Evaluated parties | Attempts | Stop reason |\n| --- | ---: | ---: | ---: | --- |");
        foreach (var arm in report.Generation?.Arms ?? [])
            text.AppendLine($"| {arm.Method} | {arm.Seed} | {arm.Evaluations.Count} | {arm.Proposals.Count} | {arm.StopReason} |");
        text.AppendLine("\n## Discovery shortlist\n\n| Recipe | Worst-context wins | Guardian health remaining | Party survival | Above observed 50% |\n| --- | ---: | ---: | ---: | --- |");
        foreach (var party in report.Generation?.DiscoveryShortlist ?? [])
        {
            var row = report.Generation!.Arms.SelectMany(a => a.Evaluations).First(r => r.Id == party.Id);
            text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {party.Id} | {row.Fitness.WorstContextWinRate:P2} | {row.Fitness.GuardianHealth:F2}% | {row.Fitness.Survival:F2}% | {row.Cells.Any(c => c.Clears.Count(x => x) * 2 > c.Clears.Count)} |"));
        }
        text.AppendLine("\nAll evaluated/rejected proposals, ordered recipes, parent/operator lineage and context outcomes are in discovery.json. Capability labels and interaction links are unconfirmed hypotheses. Method restarts and repeated recipes across arms do not create additional independent seed samples. Partial or attempt-exhausted runs do not establish equal-budget method comparisons.\n");
        return report.Generation?.Version == TowerBossImprovement.Version
            ? text.ToString().Replace("Independent team discovery", "Retained-build improvement")
                .Replace("No benchmark reference was used as a parent or scored by this pass.", "Explicit supplied references were scored on discovery seeds and used as parents. This is reference-derived search, not independent discovery.")
            : text.ToString();
    }
}
