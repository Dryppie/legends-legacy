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

    public static TowerProposalRacingPlan WithGearProfile(TowerProposalRacingPlan source,
        TowerGearProfile profile, string contentRoot, TowerSettings settings)
    {
        var plan = TowerBatchRacing.Copy(source);
        Validate(plan);
        var scope = plan.Racing.Scope;
        if (scope.SettingsHash != HarnessJson.Hash(settings) || scope.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current()))
            throw new InvalidDataException("Gear selection requires the current admitted settings and executable.");
        var captured = new LoadoutScope(TowerProposalRacingNative.ArchiveAlgorithm(plan), settings,
            ExecutionIdentity.Current(), scope.ContentHashes);
        TowerProposalRacingNative.ValidateContent(plan, contentRoot, captured, CancellationToken.None);
        var content = OfflineContent.ForTower(contentRoot, settings);
        scope = scope with {
            Contexts = scope.Contexts.Select(c => c with {
                CharacterTemplates = TowerGearProfiles.Apply(c.CharacterTemplates, profile, content) }).ToArray(),
            References = scope.References.Select(r => r with {
                Scenario = TowerGearProfiles.Apply(r.Scenario, profile, content),
                Source = $"Gear profile {profile.Id} applied to reference {r.Id}; previous strength claims do not transfer.",
                EvidenceHash = HarnessJson.Hash(new { source = r, gearProfile = profile }) }).ToArray()
        };
        var racing = plan.Racing with { Scope = scope,
            Mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(scope), plan.DamageAffinityInventory!) };
        return CreatePlan(racing, plan.DamageAffinityInventory!, plan.Policy.CreatedDamageAffinityIds!);
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
        if (args is ["tower-affinity-search-gear", var source, var catalog, var profileId, var root, var output])
        {
            token.ThrowIfCancellationRequested();
            var plan = WithGearProfile(TowerContractJson.Read<TowerProposalRacingPlan>(source),
                TowerGearProfiles.Select(TowerGearProfiles.Read(catalog), profileId), root, TowerBundle.ReadSettings(root));
            HarnessJson.WriteNew(output, plan);
            Console.WriteLine(JsonSerializer.Serialize(new { profile = Profile, gearProfile = profileId, status = "ValidPlan",
                planHash = HarnessJson.Hash(plan), plannedFights = 528, admissionRequired = true, newFights = 0 }, HarnessJson.Options));
            return 0;
        }
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
        throw new InvalidDataException("Use tower-affinity-search-check <plan.json>, tower-affinity-search-gear <plan.json> <catalog.json> <profile-id> <content-root> <new-plan.json>, or tower-affinity-search-verify <archive> <manifest-sha256>. Execution uses TowerAffinitySearch.RunAsync inside an admitted archive owner.");
    }
}
