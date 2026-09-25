using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerProposalPolicy(string Version, string Name, IReadOnlyList<string> FirstWave,
    IReadOnlyList<string> SecondWave, IReadOnlyList<string> ParentTickets, bool PreserveParentInteractions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? PreservedDamageAffinityIds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? CreatedDamageAffinityIds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CreationRemovalRule = null);
public sealed record TowerProposalContext(TowerBossDiscoveryDefinition Scope, BossGenerationMechanics Mechanics,
    string BenchmarkReferenceId, int RootSeed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerBossInventoryReport? DamageAffinityInventory = null);
public sealed record TowerProposalExportRequest(string Version, TowerProposalContext Context,
    IReadOnlyList<TowerProposalPolicy> Policies);
public sealed record TowerProposalTeam(string Role, string PartyId, TowerScenario Scenario);
public sealed record TowerProtectedParentPair(string ParentId, int Owner, string EnablerId, string ConsumerId);
public sealed record TowerProtectedParentAffinity(string ParentId, int Owner, string AffinityId,
    string ProducerEssenceId, string ModifierEssenceId);
public sealed record TowerEquivalentProposalArms(string FirstArm, string SecondArm);
public sealed record TowerProposalArmExport(string Name, string PolicyHash, string Status,
    TowerAdaptiveBatch Batch, IReadOnlyList<TowerProposalTeam> Teams, IReadOnlyList<TowerProtectedParentPair> ProtectedParentPairs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerProtectedParentAffinity>? ProtectedDamageAffinities = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAffinityCreationCoverage? AffinityCreationCoverage = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerProposalSecondWaveExport? SecondWave = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerLoadoutPlacementCatalogue? LoadoutPlacementCatalogue = null);
public sealed record TowerProposalSecondWaveExport(TowerAdaptiveBatch Batch,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAffinityCreationCoverage? AffinityCreationCoverage = null);
public sealed record TowerProposalExport(string Version, string RequestHash, string Status, string Interpretation,
    int NewFights, int NewReservedValues, IReadOnlyList<TowerProposalArmExport> Arms,
    IReadOnlyList<TowerEquivalentProposalArms> IdenticalFirstWaveRecipes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerDamageSourceAffinityReport? DamageSourceAffinities = null);
public sealed record TowerProposalRacingPlan(string Version, TowerAdaptiveRacingPlan Racing, TowerProposalPolicy Policy,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerBossInventoryReport? DamageAffinityInventory = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SelectionPolicyVersion = null);
public sealed record TowerProposalRacingReport(string Version, string PlanHash, string PolicyHash,
    IReadOnlyList<TowerAdaptiveBatch> Batches, TowerBatchRacingReport Evaluation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SelectionPolicyVersion = null);

/// <summary>Opt-in proposal experiments. Existing adaptive plans keep their original
/// JSON contract, streams and defaults. No command here prepares teams or runs combat.</summary>
public static class TowerProposalPolicies
{
    public const string Version = "tower-proposal-policy-v1";
    public const string ExportVersion = "tower-proposal-export-v1";
    public const string RacingVersion = "tower-proposal-racing-v1";
    public const string DamagePolicyVersion = "tower-proposal-policy-v2";
    public const string DamageExportVersion = "tower-proposal-export-v2";
    public const string DamageRacingVersion = "tower-proposal-racing-v2";
    public const string CreationPolicyVersion = "tower-proposal-policy-v3";
    public const string CreationExportVersion = "tower-proposal-export-v3";
    public const string CreationRacingVersion = "tower-proposal-racing-v3";
    public const string PreservingCreationPolicyVersion = "tower-proposal-policy-v4";
    public const string PreservingCreationExportVersion = "tower-proposal-export-v4";
    public const string CompletableAffinityRemovalRule = "preserve-completable-affinity-endpoints-v1";
    public const string AlliedActionPolicyVersion = "tower-proposal-policy-v5";
    public const string AlliedActionExportVersion = "tower-proposal-export-v5";
    public const string AlliedActionRemovalRule = "preserve-completable-affinity-endpoints-and-allied-basic-attack-providers-v1";
    public const string BenchmarkTieRacingVersion = "tower-proposal-racing-v4";
    public const string BenchmarkTieSelectionVersion = "tower-racing-benchmark-positive-tie-v1";
    public const string BenchmarkValidationRacingVersion = "tower-proposal-racing-v5";
    public const string PreservingValidationRacingVersion = "tower-proposal-racing-v6";
    public const string AlliedActionValidationRacingVersion = "tower-proposal-racing-v7";
    public const string LoadoutPlacementPolicyVersion = "tower-proposal-policy-v6";
    public const string LoadoutPlacementExportVersion = "tower-proposal-export-v6";
    public const string LoadoutPlacementRacingVersion = "tower-proposal-racing-v8";
    internal static bool UsesBenchmarkValidation(string version) =>
        version is BenchmarkValidationRacingVersion or PreservingValidationRacingVersion or AlliedActionValidationRacingVersion or LoadoutPlacementRacingVersion or TowerAffinitySearch.GeneratedNominationVersion;
    private const long MaximumExportBytes = 64L * 1048576;
    private static readonly string[] Operators = ["single", "coordinated", "partial", "recombine", "guided-pair", "fresh"];

    // Fresh objects prevent callers from mutating global policy definitions.
    public static TowerProposalPolicy Legacy() => new(Version, "legacy-v1",
        ["single", "single", "coordinated", "single", "partial", "recombine", "single", "guided-pair", "fresh"],
        ["single", "coordinated", "partial", "single", "recombine", "guided-pair", "single", "fresh"],
        ["benchmark", "benchmark", "other-reference", "beam"], false);

    public static TowerProposalPolicy BenchmarkSmallEdits(bool preserveInteractions = false) => new(Version,
        preserveInteractions ? "benchmark-preserving-single-v1" : "benchmark-single-v1",
        Enumerable.Repeat("single", 9).ToArray(), Enumerable.Repeat("single", 8).ToArray(),
        ["benchmark"], preserveInteractions);

    public static TowerProposalPolicy BenchmarkDamageEdits(IEnumerable<string> affinityIds) => new(DamagePolicyVersion,
        "benchmark-damage-preserving-single-v2", Enumerable.Repeat("single", 9).ToArray(),
        Enumerable.Repeat("single", 8).ToArray(), ["benchmark"], false,
        affinityIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    public static TowerProposalPolicy BenchmarkAffinityCreation(IEnumerable<string> affinityIds) => new(CreationPolicyVersion,
        "benchmark-affinity-creation-v3", Enumerable.Repeat("affinity-create", 9).ToArray(),
        Enumerable.Repeat("affinity-create", 8).ToArray(), ["benchmark"], false, [],
        affinityIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    public static TowerProposalPolicy BenchmarkPreservingAffinityCreation(IEnumerable<string> affinityIds) =>
        BenchmarkAffinityCreation(affinityIds) with {
            Version = PreservingCreationPolicyVersion, Name = "benchmark-preserving-affinity-creation-v4",
            CreationRemovalRule = CompletableAffinityRemovalRule
        };

    public static TowerProposalPolicy BenchmarkAlliedActionAffinityCreation(IEnumerable<string> affinityIds) =>
        BenchmarkPreservingAffinityCreation(affinityIds) with {
            Version = AlliedActionPolicyVersion, Name = "benchmark-allied-action-affinity-creation-v5",
            CreationRemovalRule = AlliedActionRemovalRule
        };

    public static TowerProposalPolicy BenchmarkLoadoutPlacement() => new(LoadoutPlacementPolicyVersion,
        "benchmark-subgroup-loadout-placement-v6", Enumerable.Repeat(TowerLoadoutPlacement.Operator, 9).ToArray(),
        Enumerable.Repeat(TowerLoadoutPlacement.Operator, 8).ToArray(), ["benchmark"], false);

    private static bool RequiresAffinityInventory(TowerProposalPolicy policy) =>
        policy.Version is not (Version or LoadoutPlacementPolicyVersion);

    internal static bool CreatesAffinities(TowerProposalPolicy policy) =>
        policy.Version is CreationPolicyVersion or PreservingCreationPolicyVersion or AlliedActionPolicyVersion;

    internal static bool IsLegacy(TowerProposalPolicy policy) => HarnessJson.Hash(policy) == HarnessJson.Hash(Legacy());

    public static void Validate(TowerProposalPolicy policy)
    {
        if (policy?.Version == LoadoutPlacementPolicyVersion)
        {
            if (HarnessJson.Hash(policy) != HarnessJson.Hash(BenchmarkLoadoutPlacement()))
                throw new InvalidDataException("Loadout placement requires its exact fixed benchmark-only two-wave policy.");
            return;
        }
        if (policy is null || policy.Version is not (Version or DamagePolicyVersion or CreationPolicyVersion or PreservingCreationPolicyVersion or AlliedActionPolicyVersion) || !TowerBenchmark.SafeId(policy.Name)
            || policy.FirstWave is not { Count: 9 } || policy.SecondWave is not { Count: 8 }
            || policy.FirstWave.Concat(policy.SecondWave).Any(s => !Operators.Contains(s)
                && !(CreatesAffinities(policy) && s == "affinity-create"))
            || policy.ParentTickets is not { Count: > 0 and <= 16 }
            || policy.ParentTickets.Any(s => s is not ("benchmark" or "other-reference" or "beam"))
            || policy.Name == "legacy-v1" && !IsLegacy(policy)
            || policy.Version == Version && policy.PreservedDamageAffinityIds is not null
            || policy.Version != Version && (policy.PreservedDamageAffinityIds is null
                || policy.PreservedDamageAffinityIds.Any(id => !TowerContractJson.Hash(id))
                || !policy.PreservedDamageAffinityIds.SequenceEqual(policy.PreservedDamageAffinityIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
            || policy.CreationRemovalRule != (policy.Version == AlliedActionPolicyVersion ? AlliedActionRemovalRule
                : policy.Version == PreservingCreationPolicyVersion ? CompletableAffinityRemovalRule : null)
            || !CreatesAffinities(policy) && policy.CreatedDamageAffinityIds is not null
            || CreatesAffinities(policy) && (policy.CreatedDamageAffinityIds is not { Count: > 0 and <= 32 }
                || policy.CreatedDamageAffinityIds.Any(id => !TowerContractJson.Hash(id))
                || !policy.CreatedDamageAffinityIds.SequenceEqual(policy.CreatedDamageAffinityIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                || !policy.FirstWave.Concat(policy.SecondWave).Contains("affinity-create")))
            throw new InvalidDataException("Unknown proposal policy, changed legacy preset, or invalid fixed batch/parent schedule.");
    }

    internal static TowerDamageSourceAffinity[] SelectedAffinities(TowerProposalContext context, TowerProposalPolicy policy,
        bool creation = false)
    {
        if (!RequiresAffinityInventory(policy)) return [];
        if (context.DamageAffinityInventory is null) throw new InvalidDataException("Damage affinity policy requires its full inventory evidence.");
        var inventory = context.DamageAffinityInventory;
        if (HarnessJson.Hash(inventory.SourceHashes) != HarnessJson.Hash(context.Mechanics.SourceHashes)
            || HarnessJson.Hash(inventory.Essences) != HarnessJson.Hash(context.Mechanics.Essences)
            || HarnessJson.Hash(inventory.EnablerConsumerPairs) != HarnessJson.Hash(context.Mechanics.Interactions))
            throw new InvalidDataException("Damage affinity evidence differs from the frozen mechanics.");
        var report = TowerDamageSourceAffinities.Create(inventory);
        var ids = creation ? policy.CreatedDamageAffinityIds ?? [] : policy.PreservedDamageAffinityIds!;
        var selected = report.Affinities.Where(a => ids.Contains(a.Id)).ToArray();
        var allowed = context.Scope.AllowedEssences.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        if (selected.Length != ids.Count
            || selected.Any(a => !allowed.Contains(a.ProducerEssenceId) || !allowed.Contains(a.ModifierEssenceId)))
            throw new InvalidDataException("Unknown or ineligible selected damage affinity.");
        return selected;
    }

    private static void Validate(TowerProposalContext context)
    {
        if (context is null || context.Scope is null || context.Mechanics is null)
            throw new InvalidDataException("Supply the frozen generation context and mechanics.");
        TowerBatchRacing.ValidateScope(context.Scope);
        var d = context.Scope;
        if (d.RequiredPartySize < 2 || d.Budget.EssenceSlots < 2
            || d.Starts.Count(s => s.ReferenceId == context.BenchmarkReferenceId) != 1)
            throw new InvalidDataException("Proposals require two owners/slots and an exact benchmark reference.");
        _ = new TowerBossPartyGenerator(TowerBossDiscovery.CopyGenerationInputs(d), context.Mechanics);
        if (HarnessJson.Hash(context.Mechanics.SourceHashes) != HarnessJson.Hash(TowerBossInventory.SourceFiles.ToDictionary(f => f, f => d.ContentHashes[f])))
            throw new InvalidDataException("Generation mechanics differ from the declared content hashes.");
    }

    public static void Validate(TowerProposalExportRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request is null || request.Version is not (ExportVersion or DamageExportVersion or CreationExportVersion or PreservingCreationExportVersion or AlliedActionExportVersion or LoadoutPlacementExportVersion) || request.Policies is not { Count: >= 2 and <= 4 })
            throw new InvalidDataException("Proposal export requires two to four explicit policies.");
        Validate(request.Context);
        foreach (var policy in request.Policies) Validate(policy);
        var placement = request.Policies.Any(p => p.Version == LoadoutPlacementPolicyVersion);
        var allied = request.Policies.Any(p => p.Version == AlliedActionPolicyVersion);
        var preserving = allied || request.Policies.Any(p => p.Version == PreservingCreationPolicyVersion);
        var creation = request.Policies.Any(CreatesAffinities);
        var damage = request.Policies.Any(RequiresAffinityInventory);
        var expected = placement ? LoadoutPlacementExportVersion : allied ? AlliedActionExportVersion : preserving ? PreservingCreationExportVersion : creation ? CreationExportVersion : damage ? DamageExportVersion : ExportVersion;
        if (request.Version != expected || damage != (request.Context.DamageAffinityInventory is not null))
            throw new InvalidDataException("Use the matching export version and explicit affinity inventory.");
        // V4 exports both waves without fabricated feedback. Recombination also
        // consults the measured beam for donors even with a fixed parent ticket.
        if ((preserving || placement) && request.Policies.Any(p => p.ParentTickets.Contains("beam")
            || p.FirstWave.Concat(p.SecondWave).Contains("recombine")))
            throw new InvalidDataException("Two-wave generation exports require policies independent of measured beam feedback.");
        foreach (var policy in request.Policies)
        {
            _ = SelectedAffinities(request.Context, policy);
            if (CreatesAffinities(policy)) _ = SelectedAffinities(request.Context, policy, creation: true);
        }
        if (allied) _ = TowerAlliedActionProtection.Create(request.Context.DamageAffinityInventory!);
        if (placement) _ = new TowerLoadoutPlacement(request.Context, token);
        if (request.Policies.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != request.Policies.Count)
            throw new InvalidDataException("Proposal comparison arm names must be unique.");
    }

    public static TowerProposalExport Export(TowerProposalExportRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        request = TowerBatchRacing.Copy(request);
        Validate(request, token);
        var d = request.Context.Scope;
        var twoWaves = request.Version is PreservingCreationExportVersion or AlliedActionExportVersion or LoadoutPlacementExportVersion;
        var seen = d.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        var arms = new List<TowerProposalArmExport>();
        foreach (var policy in request.Policies)
        {
            token.ThrowIfCancellationRequested();
            var generator = new TowerAdaptiveRacingGenerator(request.Context, policy, token);
            var batch = generator.Generate(1, [], seen, [], token);
            token.ThrowIfCancellationRequested();
            TowerProposalSecondWaveExport? second = null;
            if (twoWaves)
            {
                var seenAfterFirst = seen.Concat(batch.Candidates.Select(p => p.Id)).ToHashSet(StringComparer.Ordinal);
                var secondBatch = generator.Generate(2, [], seenAfterFirst, [], token);
                token.ThrowIfCancellationRequested();
                second = new(secondBatch, generator.CreationCoverage(secondBatch, token));
            }
            var parties = d.Starts.Select(s => s.Party).Concat(batch.Candidates).Concat(second?.Batch.Candidates ?? []).ToArray();
            TowerBatchRacing.ValidateParties(d, parties);
            var parents = batch.Proposals.Concat(second?.Batch.Proposals ?? [])
                .Where(p => p.Parents.Count > 0).Select(p => p.Parents[0]).ToHashSet(StringComparer.Ordinal);
            var protectedPairs = policy.PreserveParentInteractions ? d.Starts.Where(s => parents.Contains(s.Party.Id))
                .SelectMany(s => s.Party.Builds.SelectMany(b => request.Context.Mechanics.Interactions
                    .Where(p => p.Compatibility == "same-owner-or-explicit-recipient-required"
                        && b.Value.Contains(p.EnablerEssenceId) && b.Value.Contains(p.ConsumerEssenceId))
                    .Select(p => new TowerProtectedParentPair(s.Party.Id, b.Key, p.EnablerEssenceId, p.ConsumerEssenceId))))
                .Distinct().OrderBy(p => p.ParentId, StringComparer.Ordinal).ThenBy(p => p.Owner)
                .ThenBy(p => p.EnablerId, StringComparer.Ordinal).ThenBy(p => p.ConsumerId, StringComparer.Ordinal).ToArray() : [];
            var affinities = SelectedAffinities(request.Context, policy);
            var protectedDamage = RequiresAffinityInventory(policy) ? d.Starts.Where(s => parents.Contains(s.Party.Id))
                .SelectMany(s => s.Party.Builds.SelectMany(b => affinities
                    .Where(a => b.Value.Contains(a.ProducerEssenceId) && b.Value.Contains(a.ModifierEssenceId))
                    .Select(a => new TowerProtectedParentAffinity(s.Party.Id, b.Key, a.Id, a.ProducerEssenceId, a.ModifierEssenceId))))
                .OrderBy(a => a.ParentId, StringComparer.Ordinal).ThenBy(a => a.Owner).ThenBy(a => a.AffinityId, StringComparer.Ordinal).ToArray() : null;
            arms.Add(new(policy.Name, HarnessJson.Hash(policy), batch.Candidates.Count == 9 && (!twoWaves || second!.Batch.Candidates.Count == 8) ? "Complete" : "Incomplete",
                batch, parties.Select(p => new TowerProposalTeam(seen.Contains(p.Id) ? "reference" : "candidate", p.Id,
                    policy.Version == LoadoutPlacementPolicyVersion
                        ? TowerLoadoutPlacement.Scenario(d, request.Context.BenchmarkReferenceId, p)
                        : TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, p, []))).ToArray(), protectedPairs, protectedDamage,
                generator.CreationCoverage(batch, token), second, generator.PlacementCatalogue));
        }
        var ordered = arms.OrderBy(a => a.Name, StringComparer.Ordinal).ToArray();
        var identical = ordered.SelectMany((a, i) => ordered.Skip(i + 1)
            .Where(b => a.Status == "Complete" && b.Status == "Complete"
                && a.Batch.Candidates.Select(p => p.Id).SequenceEqual(b.Batch.Candidates.Select(p => p.Id)))
            .Select(b => new TowerEquivalentProposalArms(a.Name, b.Name))).ToArray();
        return new(request.Version, HarnessJson.Hash(request), arms.All(a => a.Status == "Complete") ? "Complete" : "Incomplete",
            twoWaves ? "TwoWaveGenerationOnlyNoMeasurementsOrPromotion" : "FirstWaveGenerationOnlyNoMeasurementsOrPromotion", 0, 0, arms, identical,
            request.Context.DamageAffinityInventory is null ? null : TowerDamageSourceAffinities.Create(request.Context.DamageAffinityInventory));
    }

    public static void Validate(TowerProposalRacingPlan plan, CancellationToken token = default)
    {
        // Earlier racing versions retain the kernel's original cancellation receipt.
        if (plan?.Version == LoadoutPlacementRacingVersion) token.ThrowIfCancellationRequested();
        if (plan is null || plan.Version is not (RacingVersion or DamageRacingVersion or CreationRacingVersion or BenchmarkTieRacingVersion or BenchmarkValidationRacingVersion or PreservingValidationRacingVersion or AlliedActionValidationRacingVersion or LoadoutPlacementRacingVersion or TowerAffinitySearch.GeneratedNominationVersion)
            || plan.Racing is null || plan.Racing.MaximumEvaluations != 528)
            throw new InvalidDataException("Proposal racing requires its separate version and the fixed 528-fight allocation.");
        var validation = UsesBenchmarkValidation(plan.Version);
        TowerAdaptiveRacing.Validate(plan.Racing, validation);
        Validate(plan.Policy);
        if ((plan.Policy.Version == LoadoutPlacementPolicyVersion) != (plan.Version == LoadoutPlacementRacingVersion))
            throw new InvalidDataException("Loadout placement requires its separate v8 racing contract and v6 policy.");
        if (plan.Policy.Version == AlliedActionPolicyVersion && plan.Version != AlliedActionValidationRacingVersion)
            throw new InvalidDataException("Allied-action preservation is generation-export only in earlier racing contracts; use the explicit v7 contract.");
        if (plan.Version == AlliedActionValidationRacingVersion && plan.Policy.Version != AlliedActionPolicyVersion)
            throw new InvalidDataException("The v7 racing contract requires the allied-action creation policy.");
        if (plan.Policy.Version == PreservingCreationPolicyVersion && plan.Version != PreservingValidationRacingVersion)
            throw new InvalidDataException("Preserving affinity creation is generation-export only; existing racing contracts do not admit this policy.");
        if (plan.Version == PreservingValidationRacingVersion && plan.Policy.Version != PreservingCreationPolicyVersion)
            throw new InvalidDataException("The v6 racing contract requires the preserving affinity-creation policy.");
        if (plan.Version == TowerAffinitySearch.GeneratedNominationVersion
            && (plan.Policy.CreatedDamageAffinityIds is not { Count: > 0 }
                || HarnessJson.Hash(plan.Policy) != HarnessJson.Hash(BenchmarkAffinityCreation(plan.Policy.CreatedDamageAffinityIds))))
            throw new InvalidDataException("Generated-only nomination requires the original affinity-creation policy.");
        var selector = plan.Version switch {
            BenchmarkTieRacingVersion => BenchmarkTieSelectionVersion,
            BenchmarkValidationRacingVersion or PreservingValidationRacingVersion or AlliedActionValidationRacingVersion or LoadoutPlacementRacingVersion or TowerAffinitySearch.GeneratedNominationVersion => TowerBenchmarkValidation.Version,
            _ => null
        };
        if (plan.SelectionPolicyVersion != selector)
            throw new InvalidDataException("Proposal racing requires the exact versioned output policy; earlier versions retain their original selector.");
        var expected = plan.Policy.Version switch {
            Version => RacingVersion, DamagePolicyVersion => DamageRacingVersion, _ => CreationRacingVersion
        };
        if (plan.Version is not (BenchmarkTieRacingVersion or BenchmarkValidationRacingVersion or PreservingValidationRacingVersion or AlliedActionValidationRacingVersion or LoadoutPlacementRacingVersion or TowerAffinitySearch.GeneratedNominationVersion) && plan.Version != expected
            || RequiresAffinityInventory(plan.Policy) != (plan.DamageAffinityInventory is not null))
            throw new InvalidDataException("Proposal racing requires the matching policy version and explicit affinity inventory.");
        var context = new TowerProposalContext(plan.Racing.Scope, plan.Racing.Mechanics, plan.Racing.BenchmarkReferenceId,
            plan.Racing.RootSeed, plan.DamageAffinityInventory);
        _ = SelectedAffinities(context, plan.Policy);
        if (CreatesAffinities(plan.Policy)) _ = SelectedAffinities(context, plan.Policy, creation: true);
        if (plan.Version == AlliedActionValidationRacingVersion) _ = TowerAlliedActionProtection.Create(context.DamageAffinityInventory!);
        if (plan.Version == LoadoutPlacementRacingVersion) _ = new TowerLoadoutPlacement(context, token);
    }

    // An evaluator adapter must supply admitted trials. This API neither allocates
    // panels nor hosts a native campaign. V4 overrides positive ties; V5 freezes one
    // non-benchmark nominee before a separate complete validation panel.
    public static Task<TowerProposalRacingReport> RunAsync(TowerProposalRacingPlan plan,
        TowerBatchRacing.Evaluator evaluate, CancellationToken token = default,
        Action<TowerProposalRacingReport>? checkpoint = null)
        => RunAsync(plan, evaluate, token, checkpoint, false);

    // The owned native adapter journals each dispatch itself. Reuse the kernel's
    // existing panel-freeze mode without copying the growing history per trial.
    internal static async Task<TowerProposalRacingReport> RunAsync(TowerProposalRacingPlan plan,
        TowerBatchRacing.Evaluator evaluate, CancellationToken token,
        Action<TowerProposalRacingReport>? checkpoint, bool panelFreezesOnly)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        plan = TowerBatchRacing.Copy(plan);
        Validate(plan, token);
        var p = plan.Racing;
        var planHash = HarnessJson.Hash(plan);
        var policyHash = HarnessJson.Hash(plan.Policy);
        var batches = new List<TowerAdaptiveBatch>();
        var generator = new TowerAdaptiveRacingGenerator(new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, plan.DamageAffinityInventory), plan.Policy, token);
        var seen = p.Scope.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        TowerProposalRacingReport Snapshot(TowerBatchRacingReport evaluation) => TowerBatchRacing.Copy(
            new TowerProposalRacingReport(plan.Version, planHash, policyHash, batches, evaluation, plan.SelectionPolicyVersion));
        var benchmarkId = p.Scope.Starts.Single(s => s.ReferenceId == p.BenchmarkReferenceId).Party.Id;
        var positiveTieBenchmarkId = plan.Version == BenchmarkTieRacingVersion ? benchmarkId : null;
        var validationBenchmarkId = UsesBenchmarkValidation(plan.Version) ? benchmarkId : null;
        var result = await TowerBatchRacing.RunCoreAsync(plan.Version, planHash, p.Scope, p.Panels, p.MaximumEvaluations,
            (wave, beam, panels) => {
                var batch = generator.Generate(wave, beam, seen, panels, token);
                batches.Add(batch);
                foreach (var party in batch.Candidates) seen.Add(party.Id);
                return batch.Candidates;
            }, evaluate, token, checkpoint is null ? null : evaluation => checkpoint(Snapshot(evaluation)), panelFreezesOnly,
            positiveTieBenchmarkId, validationBenchmarkId);
        return Snapshot(result);
    }

    public static async Task<TowerProposalRacingReport> ReconstructAsync(TowerProposalRacingPlan plan,
        TowerProposalRacingReport saved, CancellationToken token = default)
    {
        plan = TowerBatchRacing.Copy(plan); saved = TowerBatchRacing.Copy(saved);
        Validate(plan, token);
        if (saved.Version != plan.Version || saved.PlanHash != HarnessJson.Hash(plan) || saved.SelectionPolicyVersion != plan.SelectionPolicyVersion
            || saved.PolicyHash != HarnessJson.Hash(plan.Policy) || saved.Evaluation.Status != "Complete")
            throw new InvalidDataException("Only complete evidence from the exact proposal policy/plan can be reconstructed.");
        var observations = saved.Evaluation.Panels.SelectMany(p => p.Observations).ToArray();
        var index = 0;
        var rebuilt = await RunAsync(plan, (request, _) => {
            if (index >= observations.Length || HarnessJson.Hash(request) != HarnessJson.Hash(observations[index].Request))
                throw new InvalidDataException("Changed proposal evaluation request order or identity.");
            return Task.FromResult(observations[index++].Outcome);
        }, token);
        token.ThrowIfCancellationRequested();
        if (index != observations.Length || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(saved))
            throw new InvalidDataException("Changed proposals, feedback, allocation or selection.");
        return rebuilt;
    }

    public static string WriteExport(TowerProposalExportRequest request, string output, CancellationToken token = default)
    {
        request = TowerBatchRacing.Copy(request);
        var result = Export(request, token);
        var payload = new Dictionary<string, byte[]> {
            ["request.json"] = JsonSerializer.SerializeToUtf8Bytes(request, HarnessJson.Options),
            ["batches.json"] = JsonSerializer.SerializeToUtf8Bytes(result, HarnessJson.Options)
        };
        var files = payload.ToDictionary(p => p.Key, p => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(p.Value)));
        payload["files.json"] = JsonSerializer.SerializeToUtf8Bytes(files, HarnessJson.Options);
        if (payload.Values.Sum(b => (long)b.Length) > MaximumExportBytes)
            throw new InvalidDataException("Proposal export exceeds 64 MiB.");
        token.ThrowIfCancellationRequested();
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Proposal export already exists; never overwrite evidence.");
        Directory.CreateDirectory(output);
        foreach (var file in payload)
        {
            token.ThrowIfCancellationRequested();
            var path = Path.Combine(output, file.Key);
            using var stream = TowerWorkAccounting.WriteStream(new FileStream(path, FileMode.CreateNew, FileAccess.Write), path);
            stream.Write(file.Value);
        }
        return HarnessJson.FileHash(Path.Combine(output, "files.json"));
    }

    public static TowerProposalExport VerifyExport(string output, string manifestHash, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var paths = TowerBulkCampaign.Paths(output).ToArray();
        if (paths.Sum(p => new FileInfo(p).Length) > MaximumExportBytes
            || !paths.Select(p => Path.GetRelativePath(output, p)).Order(StringComparer.Ordinal)
                .SequenceEqual(new[] { "batches.json", "files.json", "request.json" })
            || !TowerContractJson.Hash(manifestHash) || HarnessJson.FileHash(Path.Combine(output, "files.json")) != manifestHash)
            throw new InvalidDataException("Changed export inventory or external manifest pin.");
        var files = TowerContractJson.Read<Dictionary<string, string>>(Path.Combine(output, "files.json"));
        if (!files.Keys.Order(StringComparer.Ordinal).SequenceEqual(new[] { "batches.json", "request.json" })
            || files.Any(p => HarnessJson.FileHash(Path.Combine(output, p.Key)) != p.Value))
            throw new InvalidDataException("Changed proposal export bytes.");
        var request = TowerContractJson.Read<TowerProposalExportRequest>(Path.Combine(output, "request.json"));
        var rebuilt = Export(request, token);
        if (HarnessJson.Hash(rebuilt) != HarnessJson.Hash(TowerContractJson.Read<TowerProposalExport>(Path.Combine(output, "batches.json")))
            || files.Any(p => HarnessJson.FileHash(Path.Combine(output, p.Key)) != p.Value)
            || HarnessJson.FileHash(Path.Combine(output, "files.json")) != manifestHash)
            throw new InvalidDataException("Proposal export differs from reconstruction.");
        return rebuilt;
    }

    public static int Command(string[] args, CancellationToken token = default)
    {
        if (args is ["tower-proposal-policy-affinities", var inventoryPath])
        {
            if (new FileInfo(inventoryPath).Length > MaximumExportBytes) throw new InvalidDataException("Affinity inventory exceeds 64 MiB.");
            token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(TowerDamageSourceAffinities.Create(
                TowerContractJson.Read<TowerBossInventoryReport>(inventoryPath)), HarnessJson.Options));
            return 0;
        }
        if (args is ["tower-proposal-policy-presets"])
        {
            Console.WriteLine(JsonSerializer.Serialize(new[] { Legacy(), BenchmarkSmallEdits(), BenchmarkSmallEdits(true) }, HarnessJson.Options));
            return 0;
        }
        if (args is ["tower-proposal-policy-verify", var root, var pin])
        {
            var result = VerifyExport(root, pin, token);
            Console.WriteLine(JsonSerializer.Serialize(new { result.Status, result.RequestHash, result.NewFights, result.NewReservedValues }, HarnessJson.Options));
            return 0;
        }
        if (args.Length is 2 or 3 && args[0] is "tower-proposal-policy-check" or "tower-proposal-policy-export")
        {
            if (args.Length != (args[0] == "tower-proposal-policy-check" ? 2 : 3)) throw new InvalidDataException("Wrong proposal command arguments.");
            if (new FileInfo(args[1]).Length > MaximumExportBytes) throw new InvalidDataException("Proposal request exceeds 64 MiB.");
            var request = TowerContractJson.Read<TowerProposalExportRequest>(args[1]);
            Validate(request, token); token.ThrowIfCancellationRequested();
            var exportPin = args.Length == 3 ? WriteExport(request, args[2], token) : null;
            Console.WriteLine(JsonSerializer.Serialize(new { status = exportPin is null ? "ValidGenerationRequest" : "ExportedGenerationOnly",
                requestHash = HarnessJson.Hash(request), manifestSha256 = exportPin, newFights = 0, newReservedValues = 0 }, HarnessJson.Options));
            return 0;
        }
        throw new InvalidDataException("Use tower-proposal-policy-presets, -check <request>, -export <request> <new-output>, or -verify <output> <manifest-sha256>. These commands never run combat.");
    }
}
