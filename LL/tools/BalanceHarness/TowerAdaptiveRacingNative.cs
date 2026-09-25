using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace BalanceHarness;

public sealed record TowerRacingCharge(int Ordinal, string PanelHash, string PartyId, int Seed);

/// <summary>Adapter for an already admitted, captured, exclusively owned archive.
/// It does not allocate seeds, launch workers, confirm teams or publish a campaign.</summary>
public static class TowerAdaptiveRacingNative
{
    internal static string Arm(TowerPanelTrial request) => TowerAdaptiveRacing.Version + "/" + request.PanelHash;

    public static async Task<TowerAdaptiveRacingReport> RunAsync(TowerAdaptiveRacingPlan plan, TowerLoadoutArchive archive,
        long maximumEvidenceBytes, Action checkLimits, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(archive); ArgumentNullException.ThrowIfNull(checkLimits);
        plan = TowerBatchRacing.Copy(plan);
        TowerAdaptiveRacing.Validate(plan);
        checkLimits(); token.ThrowIfCancellationRequested();
        ValidateBinding(plan, archive.CapturedScope, archive.BattleLimit, archive.Trials.Count, archive.CacheHits);
        if (maximumEvidenceBytes <= 0 || File.Exists(Path.Combine(archive.OutputRoot, "trials.jsonl"))
            || new[] { "recipes", "battles" }.Any(name => !Directory.Exists(Path.Combine(archive.OutputRoot, name))
                || Directory.EnumerateFileSystemEntries(Path.Combine(archive.OutputRoot, name)).Any())
            || HarnessJson.Hash(HarnessJson.Read<LoadoutScope>(Path.Combine(archive.OutputRoot, "scope.json"))) != HarnessJson.Hash(archive.CapturedScope))
            throw new InvalidDataException("Adaptive execution requires an unused on-disk archive, matching saved scope and positive evidence allowance.");
        var root = Path.Combine(archive.OutputRoot, "content");
        ValidateContent(plan, root, archive.CapturedScope, token);
        return await ExecuteAsync(plan, Path.Combine(archive.OutputRoot, "racing"), maximumEvidenceBytes, request => {
            checkLimits(); token.ThrowIfCancellationRequested();
            return Expected(request, archive.CapturedScope, archive.Materialize(request.Scenario, request.Seed));
        }, async (arm, stage, scenario, seed, ct) => {
            checkLimits();
            var before = archive.Trials.Count;
            var result = await archive.EvaluateAsync(arm, stage, scenario, seed, ct);
            if (archive.CacheHits != 0 || archive.Trials.Count != before + 1)
                throw new InvalidDataException("Adaptive racing requires one new archived trial per request; cache reuse is not a new observation.");
            checkLimits();
            return result;
        }, checkLimits, token);
    }

    internal static void ValidateBinding(TowerAdaptiveRacingPlan plan, LoadoutScope scope, int maximum, int existing, int hits)
    {
        if (scope.Algorithm != TowerAdaptiveRacing.Version || scope.ReportStorage is not (null or "gzip-json-v1")
            || maximum != TowerBatchRacing.PlannedEvaluations || existing != 0 || hits != 0
            || HarnessJson.Hash(scope.Settings) != plan.Scope.SettingsHash || HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(plan.Scope.ContentHashes)
            || HarnessJson.Hash(scope.Execution) != plan.Scope.ExecutionHash
            || HarnessJson.Hash(ExecutionIdentity.Current()) != plan.Scope.ExecutionHash)
            throw new InvalidDataException("Adaptive native execution needs an empty 528-trial archive with the exact version, captured settings, content and current execution identity.");
    }

    private static void ValidateContent(TowerAdaptiveRacingPlan plan, string root, LoadoutScope scope, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Captures contain data files, not the source appsettings.json. The saved
        // settings were bound above; never re-read live/default settings here.
        if (HarnessJson.Hash(TowerCompactBundle.ContentHashes(root, token)) != HarnessJson.Hash(plan.Scope.ContentHashes))
            throw new InvalidDataException("Adaptive captured content changed.");
        var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(plan.Scope),
            TowerBossInventory.Create(root, scope.Settings.Threat));
        if (HarnessJson.Hash(mechanics) != HarnessJson.Hash(plan.Mechanics))
            throw new InvalidDataException("Adaptive generation mechanics differ from captured production content.");
        token.ThrowIfCancellationRequested();
    }

    private static (LoadoutTrial Trial, int MaximumTicks) Expected(TowerPanelTrial request, LoadoutScope scope, TowerBattleInput input)
    {
        if (HarnessJson.Hash(input.Scenario) != HarnessJson.Hash(request.Scenario) || input.Rules.RandomSeed != request.Seed)
            throw new InvalidDataException("Materialized adaptive input differs from the frozen request.");
        return (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
            HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, Arm(request), input)), input.Rules.MaxTicks);
    }

    internal static TowerPanelOutcome Authenticate(TowerPanelTrial request, LoadoutTrial expected, int maximumTicks,
        LoadoutTrial trial, TowerBattleReport report, int trialOffset = 0)
    {
        var battle = report?.Battle;
        var summary = battle?.Summary;
        if (trial != expected || expected.Id != $"trial-{request.Ordinal + trialOffset:D6}" || expected.Stage != request.Role
            || expected.Seed != request.Seed || expected.Recipe != HarnessJson.Hash(request.Scenario)
            || !TowerContractJson.Hash(expected.InputHash) || !TowerContractJson.Hash(expected.CacheKey)
            || battle is null || summary is null || battle.SchemaVersion != 1 || battle.ScenarioId != request.Scenario.Id
            || battle.Seed != request.Seed || battle.TicksPerSecond != FastCombatEngine.TicksPerSecond
            || !Enum.IsDefined(summary.EngineOutcome) || !Enum.IsDefined(summary.ContentOutcome)
            || summary.DurationTicks < 0 || summary.DurationTicks > maximumTicks || maximumTicks <= 0
            || summary.DurationSeconds != summary.DurationTicks / (double)FastCombatEngine.TicksPerSecond
            || report!.DisplayDurationSeconds != (int)Math.Ceiling(summary.DurationSeconds)
            || report.Succeeded != (summary.ContentOutcome == BattleOutcome.Victory)
            || report.GuardianHealthRemainingPercent is < 0 or > 100 || summary.Friendly is not { Count: > 0 })
            throw new InvalidDataException("Adaptive battle evidence differs from its prepared input, trial identity, outcome or duration.");
        return new(HarnessJson.Hash(request), trial.Id, trial.Seed, summary.ContentOutcome,
            (double)report.GuardianHealthRemainingPercent, TowerBenchmark.Survival(report), summary.DurationSeconds);
    }

    // Injectable at the existing battle boundary: tests use literal reports, never a
    // substitute search implementation. The public wrapper supplies real input hashes.
    internal static async Task<TowerAdaptiveRacingReport> ExecuteAsync(TowerAdaptiveRacingPlan plan, string evidenceRoot,
        long maximumBytes, Func<TowerPanelTrial, (LoadoutTrial Trial, int MaximumTicks)> expected,
        TowerBossDiscoveryRun.Battle battle, Action checkLimits, CancellationToken token)
    {
        plan = TowerBatchRacing.Copy(plan); TowerAdaptiveRacing.Validate(plan);
        token.ThrowIfCancellationRequested(); checkLimits();
        using var evidence = new Evidence(evidenceRoot, maximumBytes, checkLimits);
        evidence.Put("plan.json", plan);
        var batches = 0; var panels = 0; var charged = 0;
        var result = await TowerAdaptiveRacing.RunAsync(plan, async (request, ct) => {
            checkLimits(); ct.ThrowIfCancellationRequested();
            var binding = expected(request);
            evidence.Append("inputs.jsonl", binding.Trial);
            checkLimits(); ct.ThrowIfCancellationRequested();
            var response = await battle(Arm(request), request.Role, request.Scenario, request.Seed, ct);
            return Authenticate(request, binding.Trial, binding.MaximumTicks, response.Trial, response.Report);
        }, token, progress => {
            checkLimits(); token.ThrowIfCancellationRequested();
            foreach (var batch in progress.Batches.Skip(batches)) evidence.Put($"batch-{++batches:D2}.json", batch);
            foreach (var panel in progress.Evaluation.Panels.Skip(panels)) evidence.Put($"panel-{++panels:D2}.json", panel.Freeze);
            if (progress.Evaluation.ChargedEvaluations > charged)
            {
                if (progress.Evaluation.ChargedEvaluations != charged + 1) throw new InvalidDataException("Missing adaptive attempt charge.");
                var panel = progress.Evaluation.Panels[^1];
                var index = panel.Observations.Count;
                evidence.Append("charges.jsonl", new TowerRacingCharge(++charged, HarnessJson.Hash(panel.Freeze),
                    panel.Freeze.Parties[index / panel.Freeze.Seeds.Count].Id, panel.Freeze.Seeds[index % panel.Freeze.Seeds.Count]));
            }
        });
        // Exhausted/cancelled generation can finish without reaching the next panel callback.
        foreach (var batch in result.Batches.Skip(batches)) evidence.Put($"batch-{++batches:D2}.json", batch);
        evidence.Put("search.json", result);
        return result;
    }

    /// <summary>Zero-fight verification after the owning runner publishes the complete archive inventory.</summary>
    public static async Task<TowerAdaptiveRacingReport> VerifyAsync(string output, CancellationToken token = default)
    {
        var trials = TowerLoadoutArchive.Verify(output, token);
        var evidenceRoot = Path.Combine(output, "racing");
        var plan = HarnessJson.Read<TowerAdaptiveRacingPlan>(Path.Combine(evidenceRoot, "plan.json"));
        TowerAdaptiveRacing.Validate(plan);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        ValidateBinding(plan, scope, TowerBatchRacing.PlannedEvaluations, 0, 0);
        ValidateContent(plan, Path.Combine(output, "content"), scope, token);
        var runner = new TowerBattleRunner(Path.Combine(output, "content"), new OfflineContent(Path.Combine(output, "content"), scope.Settings.Threat));
        var recipeFiles = trials.Select(t => t.Recipe + ".json").Distinct().Order(StringComparer.Ordinal);
        var battleFiles = trials.Select(t => t.Id + (scope.ReportStorage is null ? ".json" : ".json.gz")).Order(StringComparer.Ordinal);
        if (!Directory.EnumerateFileSystemEntries(Path.Combine(output, "recipes")).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(recipeFiles)
            || !Directory.EnumerateFileSystemEntries(Path.Combine(output, "battles")).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(battleFiles))
            throw new InvalidDataException("Adaptive archive has extra or missing recipes/battles.");
        return await VerifyEvidenceAsync(evidenceRoot, request => {
            var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", HarnessJson.Hash(request.Scenario) + ".json"));
            if (HarnessJson.Hash(recipe) != HarnessJson.Hash(request.Scenario)) throw new InvalidDataException("Saved adaptive recipe differs from the frozen request.");
            return Expected(request, scope, runner.CreateInput(request.Scenario, request.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks));
        },
            trials, trial => TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage), token);
    }

    internal static async Task<TowerAdaptiveRacingReport> VerifyEvidenceAsync(string root,
        Func<TowerPanelTrial, (LoadoutTrial Trial, int MaximumTicks)> expected, IReadOnlyList<LoadoutTrial> trials,
        Func<LoadoutTrial, TowerBattleReport> readBattle, CancellationToken token)
    {
        var plan = HarnessJson.Read<TowerAdaptiveRacingPlan>(Path.Combine(root, "plan.json"));
        var saved = HarnessJson.Read<TowerAdaptiveRacingReport>(Path.Combine(root, "search.json"));
        if (saved.Evaluation.Status != "Complete" || trials.Count != TowerBatchRacing.PlannedEvaluations)
            throw new InvalidDataException("Only complete native adaptive evidence can be verified.");
        var files = new[] { "plan.json", "search.json", "charges.jsonl", "inputs.jsonl", "batch-01.json", "batch-02.json" }
            .Concat(Enumerable.Range(1, 5).Select(i => $"panel-{i:D2}.json")).Order(StringComparer.Ordinal);
        if (!Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(files))
            throw new InvalidDataException("Unexpected or missing adaptive evidence files.");
        var rebuilt = await TowerAdaptiveRacing.ReconstructAsync(plan, saved, token);
        var observations = rebuilt.Evaluation.Panels.SelectMany(p => p.Observations).ToArray();
        var charges = new List<TowerRacingCharge>();
        for (var i = 0; i < observations.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            var observation = observations[i];
            var binding = expected(observation.Request);
            var authenticated = Authenticate(observation.Request, binding.Trial, binding.MaximumTicks, trials[i], readBattle(trials[i]));
            if (authenticated != observation.Outcome) throw new InvalidDataException("Adaptive outcome differs from archived battle evidence.");
            charges.Add(new(i + 1, observation.Request.PanelHash, observation.Request.PartyId, observation.Request.Seed));
        }
        void Match<T>(string file, T value)
        {
            if (HarnessJson.Hash(HarnessJson.Read<T>(Path.Combine(root, file))) != HarnessJson.Hash(value))
                throw new InvalidDataException("Adaptive freeze differs: " + file);
        }
        for (var i = 0; i < rebuilt.Batches.Count; i++) Match($"batch-{i + 1:D2}.json", rebuilt.Batches[i]);
        for (var i = 0; i < rebuilt.Evaluation.Panels.Count; i++) Match($"panel-{i + 1:D2}.json", rebuilt.Evaluation.Panels[i].Freeze);
        if (HarnessJson.Hash(ReadLines<TowerRacingCharge>("charges.jsonl")) != HarnessJson.Hash(charges)
            || HarnessJson.Hash(ReadLines<LoadoutTrial>("inputs.jsonl")) != HarnessJson.Hash(trials))
            throw new InvalidDataException("Adaptive charge or input journal differs from the trial archive.");
        return rebuilt;

        T[] ReadLines<T>(string file) => File.ReadLines(Path.Combine(root, file))
            .Select(line => JsonSerializer.Deserialize<T>(line, HarnessJson.Options)!).ToArray();
    }

    public static async Task<int> Command(string[] args, CancellationToken token = default)
    {
        if (args is ["tower-adaptive-racing-check", var path])
        {
            token.ThrowIfCancellationRequested();
            var plan = TowerContractJson.Read<TowerAdaptiveRacingPlan>(path);
            TowerAdaptiveRacing.Validate(plan);
            Console.WriteLine(JsonSerializer.Serialize(new { plan.Version, planHash = HarnessJson.Hash(plan),
                status = "ValidPlan", plannedEvaluations = TowerBatchRacing.PlannedEvaluations, fights = 0,
                admissionRequired = true }, HarnessJson.Options));
            return 0;
        }
        if (args is ["tower-adaptive-racing-verify", var output])
        {
            var report = await VerifyAsync(output, token);
            Console.WriteLine(JsonSerializer.Serialize(new { report.Version, report.PlanHash, status = "Verified",
                report.Evaluation.RawSelectedId, report.Evaluation.ChargedEvaluations, newFights = 0 }, HarnessJson.Options));
            return 0;
        }
        throw new InvalidDataException("Use tower-adaptive-racing-check <plan.json> or tower-adaptive-racing-verify <archive>. Native execution is hosted by an admitted runner; these commands do not launch it.");
    }

    internal sealed class Evidence : IDisposable
    {
        private readonly string root;
        private readonly long maximum;
        private readonly Action check;
        private readonly IDisposable lease;
        private long bytes;
        internal Evidence(string root, long maximum, Action check)
        {
            if (maximum <= 0) throw new InvalidDataException("Declare a positive adaptive evidence byte allowance.");
            this.root = root; this.maximum = maximum; this.check = check;
            lease = TowerCompactBundle.AcquireWriter(root);
            try
            {
                if (Path.Exists(root)) throw new IOException("Adaptive evidence already exists; retry and resume are forbidden.");
                Directory.CreateDirectory(root);
            }
            catch { lease.Dispose(); throw; }
        }
        internal void Put<T>(string name, T value) => Write(name, JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options), false);
        internal void ChargeExternalBytes(long count)
        {
            check();
            if (count < 0 || count > maximum - bytes) throw new InvalidDataException("Adaptive evidence byte allowance exhausted.");
            bytes += count;
        }
        internal void Append<T>(string name, T value) => Write(name,
            JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }).Concat(new byte[] { 10 }).ToArray(), true);
        private void Write(string name, byte[] data, bool append)
        {
            check();
            if (data.LongLength > maximum - bytes) throw new InvalidDataException("Adaptive evidence byte allowance exhausted.");
            var path = Path.Combine(root, name);
            using var stream = TowerWorkAccounting.WriteStream(new FileStream(path, append ? FileMode.Append : FileMode.CreateNew,
                FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough), path);
            stream.Write(data); TowerWorkAccounting.FlushToDisk(stream); bytes += data.LongLength;
        }
        public void Dispose() => lease.Dispose();
    }
}
