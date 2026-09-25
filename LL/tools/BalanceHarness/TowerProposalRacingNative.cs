using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

/// <summary>Adapter for an already admitted, captured, exclusively owned archive.
/// It does not allocate seeds, launch workers, confirm teams or publish a campaign.</summary>
public static class TowerProposalRacingNative
{
    private static void ValidateNativePlan(TowerProposalRacingPlan plan) => TowerProposalPolicies.Validate(plan);
    public static string ArchiveAlgorithm(TowerProposalRacingPlan plan) => plan.Version + "/" + HarnessJson.Hash(plan);
    internal static string Arm(string algorithm, TowerPanelTrial request) => algorithm + "/" + request.PanelHash;

    public static async Task<TowerProposalRacingReport> RunAsync(TowerProposalRacingPlan plan, TowerLoadoutArchive archive,
        long maximumEvidenceBytes, Action checkLimits, CancellationToken token = default, Action<bool>? attempt = null,
        TowerProposalEvidenceStorage.Writer? storage = null)
    {
        ArgumentNullException.ThrowIfNull(archive); ArgumentNullException.ThrowIfNull(checkLimits);
        plan = TowerBatchRacing.Copy(plan);
        checkLimits(); token.ThrowIfCancellationRequested();
        Preflight(plan, archive, maximumEvidenceBytes, token);
        return await ExecuteAsync(plan, Path.Combine(archive.OutputRoot, "racing"), maximumEvidenceBytes, request => {
            checkLimits(); token.ThrowIfCancellationRequested();
            return Expected(request, archive.CapturedScope, archive.Materialize(request.Scenario, request.Seed));
        }, async (arm, stage, scenario, seed, ct) => {
            checkLimits();
            var before = archive.Trials.Count;
            var result = await archive.EvaluateAsync(arm, stage, scenario, seed, ct);
            if (archive.CacheHits != 0 || archive.Trials.Count != before + 1)
                throw new InvalidDataException("Proposal racing requires one new archived trial per request; cache reuse is not a new observation.");
            checkLimits();
            return result;
        }, checkLimits, token, attempt, storage);
    }

    internal static void Preflight(TowerProposalRacingPlan plan, TowerLoadoutArchive archive, long maximumEvidenceBytes,
        CancellationToken token)
    {
        ValidateNativePlan(plan);
        ValidateBinding(plan, archive.CapturedScope, archive.BattleLimit, archive.Trials.Count, archive.CacheHits);
        if (maximumEvidenceBytes <= 0 || File.Exists(Path.Combine(archive.OutputRoot, "trials.jsonl"))
            || Path.Exists(Path.Combine(archive.OutputRoot, "racing"))
            || new[] { "recipes", "battles" }.Any(name => !Directory.Exists(Path.Combine(archive.OutputRoot, name))
                || Directory.EnumerateFileSystemEntries(Path.Combine(archive.OutputRoot, name)).Any())
            || HarnessJson.Hash(HarnessJson.Read<LoadoutScope>(Path.Combine(archive.OutputRoot, "scope.json"))) != HarnessJson.Hash(archive.CapturedScope))
            throw new InvalidDataException("Proposal execution requires an unused on-disk archive, matching saved scope and positive evidence allowance.");
        var root = Path.Combine(archive.OutputRoot, "content");
        ValidateContent(plan, root, archive.CapturedScope, token);
    }

    internal static void ValidateBinding(TowerProposalRacingPlan plan, LoadoutScope scope, int maximum, int existing, int hits)
    {
        if (scope.Algorithm != ArchiveAlgorithm(plan) || scope.ReportStorage is not (null or "gzip-json-v1")
            || maximum != TowerBatchRacing.PlannedEvaluations || existing != 0 || hits != 0
            || HarnessJson.Hash(scope.Settings) != plan.Racing.Scope.SettingsHash || HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(plan.Racing.Scope.ContentHashes)
            || HarnessJson.Hash(scope.Execution) != plan.Racing.Scope.ExecutionHash
            || HarnessJson.Hash(ExecutionIdentity.Current()) != plan.Racing.Scope.ExecutionHash)
            throw new InvalidDataException("Proposal native execution needs an empty 528-trial archive with the exact version, captured settings, content and current execution identity.");
    }

    internal static void ValidateContent(TowerProposalRacingPlan plan, string root, LoadoutScope scope, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // Captures contain data files, not the source appsettings.json. The saved
        // settings were bound above; never re-read live/default settings here.
        if (HarnessJson.Hash(TowerCompactBundle.ContentHashes(root, token)) != HarnessJson.Hash(plan.Racing.Scope.ContentHashes))
            throw new InvalidDataException("Proposal captured content changed.");
        var inventory = TowerBossInventory.Create(root, scope.Settings.Threat);
        var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(plan.Racing.Scope), inventory);
        ValidateInventory(plan, inventory);
        if (HarnessJson.Hash(mechanics) != HarnessJson.Hash(plan.Racing.Mechanics))
            throw new InvalidDataException("Proposal generation mechanics differ from captured production content.");
        token.ThrowIfCancellationRequested();
    }

    internal static void ValidateInventory(TowerProposalRacingPlan plan, TowerBossInventoryReport inventory)
    {
        // Source hash labels are not proof that the supplied typed nodes came from
        // those files. Compare the entire independently rebuilt content inventory.
        if (plan.DamageAffinityInventory is not null
            && HarnessJson.Hash(plan.DamageAffinityInventory) != HarnessJson.Hash(inventory))
            throw new InvalidDataException("Proposal affinity inventory differs from captured production content.");
    }

    private static void CheckPin(string output, string pin)
    {
        if (!TowerContractJson.Hash(pin) || HarnessJson.FileHash(Path.Combine(output, "files.json")) != pin)
            throw new InvalidDataException("Changed proposal archive external manifest pin.");
    }

    private static (LoadoutTrial Trial, int MaximumTicks) Expected(TowerPanelTrial request, LoadoutScope scope, TowerBattleInput input)
    {
        if (HarnessJson.Hash(input.Scenario) != HarnessJson.Hash(request.Scenario) || input.Rules.RandomSeed != request.Seed)
            throw new InvalidDataException("Materialized proposal input differs from the frozen request.");
        return (new($"trial-{request.Ordinal:D6}", request.Role, HarnessJson.Hash(request.Scenario), request.Seed,
            HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, Arm(scope.Algorithm, request), input)), input.Rules.MaxTicks);
    }

    // Injectable at the existing battle boundary: tests use literal reports, never a
    // substitute search implementation. The public wrapper supplies real input hashes.
    internal static async Task<TowerProposalRacingReport> ExecuteAsync(TowerProposalRacingPlan plan, string evidenceRoot,
        long maximumBytes, Func<TowerPanelTrial, (LoadoutTrial Trial, int MaximumTicks)> expected,
        TowerBossDiscoveryRun.Battle battle, Action checkLimits, CancellationToken token, Action<bool>? attempt = null,
        TowerProposalEvidenceStorage.Writer? storage = null)
    {
        plan = TowerBatchRacing.Copy(plan); ValidateNativePlan(plan);
        token.ThrowIfCancellationRequested(); checkLimits();
        using var evidence = new TowerAdaptiveRacingNative.Evidence(evidenceRoot, maximumBytes, checkLimits);
        evidence.Put("plan.json", plan);
        var algorithm = ArchiveAlgorithm(plan);
        var panelFreezesOnly = attempt is not null;
        var batches = 0; var panels = 0; var charged = 0;
        string? validationFreezeHash = null;
        var result = await TowerProposalPolicies.RunAsync(plan, async (request, ct) => {
            checkLimits(); ct.ThrowIfCancellationRequested();
            if (panelFreezesOnly)
            {
                if (request.Ordinal != charged + 1) throw new InvalidDataException("Missing owned proposal attempt charge.");
                evidence.Append("charges.jsonl", new TowerRacingCharge(++charged, request.PanelHash, request.PartyId, request.Seed));
            }
            attempt?.Invoke(false);
            var binding = expected(request);
            evidence.Append("inputs.jsonl", binding.Trial);
            checkLimits(); ct.ThrowIfCancellationRequested();
            var response = await battle(Arm(algorithm, request), request.Role, request.Scenario, request.Seed, ct);
            var authenticated = TowerAdaptiveRacingNative.Authenticate(request, binding.Trial, binding.MaximumTicks, response.Trial, response.Report);
            attempt?.Invoke(true);
            return authenticated;
        }, token, progress => {
            checkLimits(); token.ThrowIfCancellationRequested();
            foreach (var batch in progress.Batches.Skip(batches)) evidence.Put($"batch-{++batches:D2}.json", batch);
            if (progress.Evaluation.ValidationFreeze is { } frozen)
            {
                var hash = HarnessJson.Hash(frozen);
                if (validationFreezeHash is null)
                {
                    // Durable challenger identity precedes even the validation panel
                    // freeze, its first attempt charge, input preparation and dispatch.
                    evidence.Put("validation-freeze.json", frozen);
                    validationFreezeHash = hash;
                }
                else if (hash != validationFreezeHash)
                    throw new InvalidDataException("Validation challenger changed after its durable freeze.");
            }
            foreach (var panel in progress.Evaluation.Panels.Skip(panels)) evidence.Put($"panel-{++panels:D2}.json", panel.Freeze);
            if (!panelFreezesOnly && progress.Evaluation.ChargedEvaluations > charged)
            {
                if (progress.Evaluation.ChargedEvaluations != charged + 1) throw new InvalidDataException("Missing proposal attempt charge.");
                var panel = progress.Evaluation.Panels[^1];
                var index = panel.Observations.Count;
                evidence.Append("charges.jsonl", new TowerRacingCharge(++charged, HarnessJson.Hash(panel.Freeze),
                    panel.Freeze.Parties[index / panel.Freeze.Seeds.Count].Id, panel.Freeze.Seeds[index % panel.Freeze.Seeds.Count]));
            }
        }, panelFreezesOnly);
        // Exhausted/cancelled generation can finish without reaching the next panel callback.
        foreach (var batch in result.Batches.Skip(batches)) evidence.Put($"batch-{++batches:D2}.json", batch);
        if (result.Evaluation.ValidationDecision is { } decision) evidence.Put("validation-decision.json", decision);
        if (storage is null) evidence.Put("search.json", result);
        else
        {
            storage.Put(evidenceRoot, "search.json", result, evidence.ChargeExternalBytes);
            storage.SealDirectory(evidenceRoot, ["search.json"], (name, value) => evidence.Put(name, value));
        }
        return result;
    }

    /// <summary>Zero-fight verification after the owning runner publishes the complete archive inventory.</summary>
    public static async Task<TowerProposalRacingReport> VerifyAsync(string output, string manifestSha256, CancellationToken token = default,
        TowerProposalEvidenceStorage.Reader? storage = null)
    {
        CheckPin(output, manifestSha256);
        var evidenceRoot = Path.Combine(output, "racing");
        var plan = HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(evidenceRoot, "plan.json"));
        ValidateNativePlan(plan);
        var trials = TowerLoadoutArchive.Verify(output, token);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        ValidateBinding(plan, scope, TowerBatchRacing.PlannedEvaluations, 0, 0);
        ValidateContent(plan, Path.Combine(output, "content"), scope, token);
        var runner = new TowerBattleRunner(Path.Combine(output, "content"), new OfflineContent(Path.Combine(output, "content"), scope.Settings.Threat));
        var recipeFiles = trials.Select(t => t.Recipe + ".json").Distinct().Order(StringComparer.Ordinal);
        var battleFiles = trials.Select(t => t.Id + (scope.ReportStorage is null ? ".json" : ".json.gz")).Order(StringComparer.Ordinal);
        if (!Directory.EnumerateFileSystemEntries(Path.Combine(output, "recipes")).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(recipeFiles)
            || !Directory.EnumerateFileSystemEntries(Path.Combine(output, "battles")).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(battleFiles))
            throw new InvalidDataException("Proposal archive has extra or missing recipes/battles.");
        var result = await VerifyEvidenceAsync(evidenceRoot, request => {
            var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", HarnessJson.Hash(request.Scenario) + ".json"));
            if (HarnessJson.Hash(recipe) != HarnessJson.Hash(request.Scenario)) throw new InvalidDataException("Saved proposal recipe differs from the frozen request.");
            return Expected(request, scope, runner.CreateInput(request.Scenario, request.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks));
        },
            trials, trial => TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage), token, storage);
        _ = TowerLoadoutArchive.Verify(output, token);
        CheckPin(output, manifestSha256);
        return result;
    }

    internal static async Task<TowerProposalRacingReport> VerifyEvidenceAsync(string root,
        Func<TowerPanelTrial, (LoadoutTrial Trial, int MaximumTicks)> expected, IReadOnlyList<LoadoutTrial> trials,
        Func<LoadoutTrial, TowerBattleReport> readBattle, CancellationToken token, TowerProposalEvidenceStorage.Reader? storage = null)
    {
        var plan = HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(root, "plan.json"));
        ValidateNativePlan(plan);
        var validation = TowerProposalPolicies.UsesBenchmarkValidation(plan.Version);
        var files = new[] { "plan.json", "search.json", "charges.jsonl", "inputs.jsonl", "batch-01.json", "batch-02.json" }
            .Concat(Enumerable.Range(1, validation ? 6 : 5).Select(i => $"panel-{i:D2}.json"))
            .Concat(validation ? new[] { "validation-freeze.json", "validation-decision.json" } : [])
            .Order(StringComparer.Ordinal);
        var physicalFiles = storage is null ? files : storage.PhysicalMembers(root, files).Order(StringComparer.Ordinal);
        if (!Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(physicalFiles))
            throw new InvalidDataException("Unexpected or missing proposal evidence files.");
        var saved = storage is null ? HarnessJson.Read<TowerProposalRacingReport>(Path.Combine(root, "search.json"))
            : storage.Read<TowerProposalRacingReport>(root, "search.json", token);
        if (saved.Evaluation.Status != "Complete" || trials.Count != TowerBatchRacing.PlannedEvaluations)
            throw new InvalidDataException("Only complete native proposal evidence can be verified.");
        var rebuilt = await TowerProposalPolicies.ReconstructAsync(plan, saved, token);
        var observations = rebuilt.Evaluation.Panels.SelectMany(p => p.Observations).ToArray();
        var charges = new List<TowerRacingCharge>();
        for (var i = 0; i < observations.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            var observation = observations[i];
            var binding = expected(observation.Request);
            var authenticated = TowerAdaptiveRacingNative.Authenticate(observation.Request, binding.Trial, binding.MaximumTicks, trials[i], readBattle(trials[i]));
            if (authenticated != observation.Outcome) throw new InvalidDataException("Proposal outcome differs from archived battle evidence.");
            TowerWorkAccounting.Add("reconstructedTrialBindings");
            charges.Add(new(i + 1, observation.Request.PanelHash, observation.Request.PartyId, observation.Request.Seed));
        }
        void Match<T>(string file, T value)
        {
            if (HarnessJson.Hash(HarnessJson.Read<T>(Path.Combine(root, file))) != HarnessJson.Hash(value))
                throw new InvalidDataException("Proposal freeze differs: " + file);
        }
        for (var i = 0; i < rebuilt.Batches.Count; i++) Match($"batch-{i + 1:D2}.json", rebuilt.Batches[i]);
        for (var i = 0; i < rebuilt.Evaluation.Panels.Count; i++) Match($"panel-{i + 1:D2}.json", rebuilt.Evaluation.Panels[i].Freeze);
        if (validation)
        {
            Match("validation-freeze.json", rebuilt.Evaluation.ValidationFreeze);
            Match("validation-decision.json", rebuilt.Evaluation.ValidationDecision);
        }
        if (HarnessJson.Hash(ReadLines<TowerRacingCharge>("charges.jsonl")) != HarnessJson.Hash(charges)
            || HarnessJson.Hash(ReadLines<LoadoutTrial>("inputs.jsonl")) != HarnessJson.Hash(trials))
            throw new InvalidDataException("Proposal charge or input journal differs from the trial archive.");
        TowerWorkAccounting.Add("reconstructedTrajectories");
        return rebuilt;

        T[] ReadLines<T>(string file) => TowerWorkAccounting.ReadLines(Path.Combine(root, file))
            .Select(line => TowerWorkAccounting.Parse<T>(line, HarnessJson.Options)).ToArray();
    }

    public static async Task<int> Command(string[] args, CancellationToken token = default)
    {
        if (args is ["tower-proposal-racing-check", var path])
        {
            token.ThrowIfCancellationRequested();
            var plan = TowerContractJson.Read<TowerProposalRacingPlan>(path);
            TowerProposalPolicies.Validate(plan);
            Console.WriteLine(JsonSerializer.Serialize(new { plan.Version, planHash = HarnessJson.Hash(plan), policyHash = HarnessJson.Hash(plan.Policy),
                plan.SelectionPolicyVersion,
                status = "ValidPlan",
                nativeExecutionSupported = TowerProposalPolicies.UsesBenchmarkValidation(plan.Version) ? (bool?)true : null,
                plannedEvaluations = TowerBatchRacing.PlannedEvaluations, fights = 0,
                admissionRequired = true }, new JsonSerializerOptions(HarnessJson.Options) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            return 0;
        }
        if (args is ["tower-proposal-racing-verify", var output, var pin])
        {
            var report = await VerifyAsync(output, pin, token);
            Console.WriteLine(JsonSerializer.Serialize(new { report.Version, report.PlanHash, report.PolicyHash, status = "Verified",
                report.SelectionPolicyVersion,
                report.Evaluation.RawSelectedId, report.Evaluation.ChargedEvaluations, newFights = 0 },
                new JsonSerializerOptions(HarnessJson.Options) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            return 0;
        }
        throw new InvalidDataException("Use tower-proposal-racing-check <plan.json> or tower-proposal-racing-verify <archive> <manifest-sha256>. Native execution is hosted by an admitted runner; these commands do not launch it.");
    }

}
