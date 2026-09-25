using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerAffinitySearchSummary(string Profile, string Status, string BenchmarkId,
    string? SelectedId, string? ChallengerId, int Fights, bool NeedsIndependentConfirmation,
    int? GainedWins, int? LostWins);

/// <summary>The supported composition-search profile. Experimental nomination
/// stays explicit; execution uses the existing admitted native archive owner.</summary>
public static class TowerAffinitySearch
{
    public const string Profile = "affinity-creation-with-benchmark-validation-v1";
    public const string GeneratedNominationVersion = "tower-proposal-racing-v9";

    public static TowerProposalRacingPlan CreatePlan(TowerAdaptiveRacingPlan racing,
        TowerBossInventoryReport inventory, IEnumerable<string> affinityIds)
    {
        var plan = new TowerProposalRacingPlan(TowerProposalPolicies.BenchmarkValidationRacingVersion,
            TowerBatchRacing.Copy(racing), TowerProposalPolicies.BenchmarkAffinityCreation(affinityIds),
            TowerBatchRacing.Copy(inventory), TowerBenchmarkValidation.Version);
        Validate(plan);
        return plan;
    }

    public static void Validate(TowerProposalRacingPlan plan)
    {
        TowerProposalPolicies.Validate(plan);
        if (plan.Version != TowerProposalPolicies.BenchmarkValidationRacingVersion
            || plan.Policy.CreatedDamageAffinityIds is not { Count: > 0 }
            || HarnessJson.Hash(plan.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAffinityCreation(plan.Policy.CreatedDamageAffinityIds)))
            throw new InvalidDataException("Supported affinity search requires original affinity creation and unchanged benchmark validation.");
    }

    public static async Task<TowerAffinitySearchSummary> RunAsync(TowerProposalRacingPlan plan,
        TowerLoadoutArchive archive, long maximumEvidenceBytes, Action checkLimits,
        CancellationToken token = default, Action<bool>? attempt = null)
    {
        plan = TowerBatchRacing.Copy(plan); Validate(plan);
        var report = await TowerProposalRacingNative.RunAsync(plan, archive, maximumEvidenceBytes, checkLimits, token, attempt);
        return Summarize(plan, report);
    }

    internal static TowerAffinitySearchSummary Summarize(TowerProposalRacingPlan plan, TowerProposalRacingReport report)
    {
        Validate(plan);
        if (report.PlanHash != HarnessJson.Hash(plan) || report.PolicyHash != HarnessJson.Hash(plan.Policy)
            || report.Version != plan.Version || report.SelectionPolicyVersion != plan.SelectionPolicyVersion)
            throw new InvalidDataException("Search result is not bound to the supported plan.");
        var benchmark = plan.Racing.Scope.Starts.Single(s => s.ReferenceId == plan.Racing.BenchmarkReferenceId).Party.Id;
        var e = report.Evaluation;
        if (e.Status != "Complete")
            return new(Profile, e.Status, benchmark, null, null, e.ChargedEvaluations, false, null, null);
        var decision = e.ValidationDecision ?? throw new InvalidDataException("Complete search lacks validation.");
        if (e.ChargedEvaluations != 528 || e.RawSelectedId != decision.SelectedId
            || e.ValidationFreeze is null || e.ValidationFreeze.BenchmarkId != benchmark
            || decision != TowerBenchmarkValidation.Decide(e.ValidationFreeze, e.Panels.Last()))
            throw new InvalidDataException("Complete search has inconsistent validation evidence.");
        return new(Profile, decision.Passed ? "ChallengerNeedsConfirmation" : "BenchmarkRetained", benchmark,
            decision.SelectedId, e.ValidationFreeze.ChallengerId, e.ChargedEvaluations,
            decision.Passed, decision.GainedWins, decision.LostWins);
    }

    public static async Task<int> Command(string[] args, CancellationToken token)
    {
        if (args is ["tower-affinity-search-check", var path])
        {
            token.ThrowIfCancellationRequested();
            var plan = TowerContractJson.Read<TowerProposalRacingPlan>(path); Validate(plan);
            Console.WriteLine(JsonSerializer.Serialize(new { profile = Profile, status = "ValidPlan",
                planHash = HarnessJson.Hash(plan), plannedFights = 528, admissionRequired = true, newFights = 0 }, HarnessJson.Options));
            return 0;
        }
        if (args is ["tower-affinity-search-verify", var archive, var pin])
        {
            var report = await TowerProposalRacingNative.VerifyAsync(archive, pin, token);
            var plan = HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(archive, "racing/plan.json"));
            Console.WriteLine(JsonSerializer.Serialize(Summarize(plan, report), HarnessJson.Options));
            return 0;
        }
        throw new InvalidDataException("Use tower-affinity-search-check <plan.json> or tower-affinity-search-verify <archive> <manifest-sha256>. Execution uses TowerAffinitySearch.RunAsync inside an admitted archive owner.");
    }
}
