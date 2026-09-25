using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerAdaptiveRacingPlan(string Version, TowerBossDiscoveryDefinition Scope,
    BossGenerationMechanics Mechanics, string BenchmarkReferenceId, int RootSeed,
    IReadOnlyList<TowerRacingPanel> Panels, int MaximumEvaluations);
public sealed record TowerAdaptiveProposal(int Attempt, string RequestedOperator, string EffectiveOperator,
    string ParentSource, IReadOnlyList<string> Parents, IReadOnlyList<int> ScheduledOwners,
    IReadOnlyList<int> ChangedOwners, int ConstructionChecks, int ReplacementDistance,
    string? Interaction, string? Fallback, PartyChoice? Party, string? Rejection,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAffinityCreationStep? AffinityCreation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerLoadoutPlacementStep? LoadoutPlacement = null);
public sealed record TowerAdaptiveBatch(int Wave, int AfterEvaluations, IReadOnlyList<string> FeedbackPanels,
    IReadOnlyList<string> BeamIds, IReadOnlyList<string> SeenBefore,
    IReadOnlyList<TowerAdaptiveProposal> Proposals, IReadOnlyList<PartyChoice> Candidates);
public sealed record TowerAdaptiveRacingReport(string Version, string PlanHash,
    IReadOnlyList<TowerAdaptiveBatch> Batches, TowerBatchRacingReport Evaluation);

/// <summary>Reference-seeded legal neighborhoods with two adaptive batches. Training
/// output remains provisional; no confirmation outcome enters generation or selection.</summary>
public static class TowerAdaptiveRacing
{
    public const string Version = "tower-adaptive-beam-racing-v1";
    public const int MaximumAttemptsPerWave = 128;
    public const int MaximumConstructionChecks = 32;

    public static void Validate(TowerAdaptiveRacingPlan plan)
        => Validate(plan, benchmarkValidation: false);

    internal static void Validate(TowerAdaptiveRacingPlan plan, bool benchmarkValidation)
    {
        if (plan is null || plan.Version != Version || plan.Mechanics is null)
            throw new InvalidDataException("Unknown adaptive racing plan or missing frozen mechanics.");
        TowerBatchRacing.ValidateScopeAndPanels(plan.Scope, plan.Panels, plan.MaximumEvaluations, benchmarkValidation);
        var d = plan.Scope;
        if (d.RequiredPartySize < 2 || d.Budget.EssenceSlots < 2
            || d.Starts.Count(s => s.ReferenceId == plan.BenchmarkReferenceId) != 1
            || plan.Panels.SelectMany(p => p.Seeds).Contains(plan.RootSeed))
            throw new InvalidDataException("Adaptive racing requires two owners/slots, an exact benchmark reference and a separate proposal seed.");
        var inputs = TowerBossDiscovery.CopyGenerationInputs(d);
        _ = new TowerBossPartyGenerator(inputs, plan.Mechanics); // Existing family/interaction validation, zero preparation.
        if (HarnessJson.Hash(plan.Mechanics.SourceHashes) != HarnessJson.Hash(TowerBossInventory.SourceFiles
                .ToDictionary(f => f, f => d.ContentHashes[f])))
            throw new InvalidDataException("Generation mechanics must bind the scope's captured content.");
    }

    public static Task<TowerAdaptiveRacingReport> RunAsync(TowerAdaptiveRacingPlan plan,
        TowerBatchRacing.Evaluator evaluate, CancellationToken token = default,
        Action<TowerAdaptiveRacingReport>? checkpoint = null)
        => RunAsync(plan, evaluate, token, checkpoint, false);

    // Owned comparisons persist every attempt in their global journal and need only
    // immutable panel/batch freezes here. Standalone callers keep every checkpoint.
    internal static async Task<TowerAdaptiveRacingReport> RunAsync(TowerAdaptiveRacingPlan plan,
        TowerBatchRacing.Evaluator evaluate, CancellationToken token,
        Action<TowerAdaptiveRacingReport>? checkpoint, bool panelFreezesOnly)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        plan = TowerBatchRacing.Copy(plan);
        Validate(plan);
        var planHash = HarnessJson.Hash(plan);
        var batches = new List<TowerAdaptiveBatch>();
        var generator = new TowerAdaptiveRacingGenerator(plan);
        var seen = plan.Scope.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        TowerAdaptiveRacingReport Snapshot(TowerBatchRacingReport evaluation) => TowerBatchRacing.Copy(
            new TowerAdaptiveRacingReport(Version, planHash, batches, evaluation));
        var result = await TowerBatchRacing.RunCoreAsync(Version, planHash, plan.Scope, plan.Panels, plan.MaximumEvaluations,
            (wave, beam, panels) => {
                var batch = generator.Generate(wave, beam, seen, panels, token);
                batches.Add(batch);
                foreach (var party in batch.Candidates) seen.Add(party.Id);
                return batch.Candidates;
            }, evaluate, token, checkpoint is null ? null : evaluation => checkpoint(Snapshot(evaluation)), panelFreezesOnly);
        return Snapshot(result);
    }

    public static async Task<TowerAdaptiveRacingReport> ReconstructAsync(TowerAdaptiveRacingPlan plan,
        TowerAdaptiveRacingReport saved, CancellationToken token = default)
    {
        plan = TowerBatchRacing.Copy(plan); saved = TowerBatchRacing.Copy(saved);
        Validate(plan);
        if (saved.Version != Version || saved.PlanHash != HarnessJson.Hash(plan) || saved.Evaluation.Status != "Complete")
            throw new InvalidDataException("Only a complete adaptive report from this exact plan can be reconstructed.");
        var observations = saved.Evaluation.Panels.SelectMany(p => p.Observations).ToArray();
        var index = 0;
        var rebuilt = await RunAsync(plan, (request, _) => {
            if (index >= observations.Length || HarnessJson.Hash(request) != HarnessJson.Hash(observations[index].Request))
                throw new InvalidDataException("Adaptive archive request order or identity differs.");
            return Task.FromResult(observations[index++].Outcome);
        }, token);
        token.ThrowIfCancellationRequested();
        if (index != observations.Length || HarnessJson.Hash(saved) != HarnessJson.Hash(rebuilt))
            throw new InvalidDataException("Adaptive proposals, feedback, panels, decisions or accounting differ from reconstruction.");
        return rebuilt;
    }
}
