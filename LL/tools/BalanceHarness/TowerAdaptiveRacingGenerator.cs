using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

// No measurements, confirmation seeds or historical rates are accepted by proposal
// operators. The only adaptive parent input is the preceding complete wave's beam.
internal sealed class TowerAdaptiveRacingGenerator
{
    private readonly int rootSeed;
    private readonly TowerProposalPolicy policy;
    private readonly string sourceVersion;
    private readonly BossDiscoveryInputs input;
    private readonly PartyChoice[] references;
    private readonly PartyChoice benchmark;
    private readonly Dictionary<string, string> families;
    private readonly string[] pool;
    private readonly TowerEnablerConsumerPair[] interactions;
    private readonly TowerEnablerConsumerPair[] protectedInteractions;
    private readonly TowerDamageSourceAffinity[] protectedAffinities;
    private readonly TowerAffinityCreation? affinityCreation;
    private readonly TowerLoadoutPlacement? loadoutPlacement;
    internal TowerLoadoutPlacementCatalogue? PlacementCatalogue => loadoutPlacement?.Catalogue;
    private bool Preserve => policy.PreserveParentInteractions || protectedAffinities.Length != 0;
    private readonly Dictionary<(string Parent, int Owner), HashSet<string>> protectedAssignments = [];
    private readonly Dictionary<string, (int[] Order, int Cursor)> ownerSchedules = new(StringComparer.Ordinal);
    internal TowerAdaptiveRacingGenerator(TowerAdaptiveRacingPlan plan)
        : this(new(plan.Scope, plan.Mechanics, plan.BenchmarkReferenceId, plan.RootSeed), TowerProposalPolicies.Legacy()) { }

    internal TowerAdaptiveRacingGenerator(TowerProposalContext context, TowerProposalPolicy policy, CancellationToken token = default)
    {
        context = TowerBatchRacing.Copy(context);
        TowerProposalPolicies.Validate(policy);
        this.policy = TowerBatchRacing.Copy(policy);
        if (policy.Version == TowerProposalPolicies.LoadoutPlacementPolicyVersion)
            loadoutPlacement = new(context, token);
        rootSeed = context.RootSeed;
        sourceVersion = TowerProposalPolicies.IsLegacy(policy) ? TowerAdaptiveRacing.Version : policy.Version;
        protectedAffinities = TowerProposalPolicies.SelectedAffinities(context, this.policy);
        input = TowerBossDiscovery.CopyGenerationInputs(context.Scope);
        references = context.Scope.Starts.Select(s => s.Party).ToArray();
        benchmark = context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party;
        families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        pool = families.Keys.Order(StringComparer.Ordinal).ToArray();
        interactions = context.Mechanics.Interactions.OrderBy(p => p.EnablerEssenceId, StringComparer.Ordinal)
            .ThenBy(p => p.ConsumerEssenceId, StringComparer.Ordinal).ThenBy(HarnessJson.Hash, StringComparer.Ordinal)
            .DistinctBy(p => (p.EnablerEssenceId, p.ConsumerEssenceId)).ToArray();
        protectedInteractions = context.Mechanics.Interactions
            .Where(p => p.Compatibility == "same-owner-or-explicit-recipient-required").ToArray();
        if (TowerProposalPolicies.CreatesAffinities(policy))
            affinityCreation = new(input, TowerProposalPolicies.SelectedAffinities(context, policy, creation: true), Protected,
                policy.Version is TowerProposalPolicies.PreservingCreationPolicyVersion or TowerProposalPolicies.AlliedActionPolicyVersion,
                policy.Version == TowerProposalPolicies.AlliedActionPolicyVersion
                    ? TowerAlliedActionProtection.Create(context.DamageAffinityInventory!) : null);
    }

    private Random Stream(string purpose, params string[] keys) => new(StableRandom.Seed(
        new[] { TowerAdaptiveRacing.Version, rootSeed.ToString(CultureInfo.InvariantCulture), purpose }.Concat(keys).ToArray()));

    private int[] Owners(string operation, string parent, int count)
    {
        var key = operation + "/" + parent;
        if (!ownerSchedules.TryGetValue(key, out var state))
        {
            var order = Enumerable.Range(1, input.RequiredPartySize).ToArray();
            Stream("owners", operation, parent).Shuffle(order);
            state = (order, 0);
        }
        var result = Enumerable.Range(0, count).Select(i => state.Order[(state.Cursor + i) % state.Order.Length]).ToArray();
        ownerSchedules[key] = (state.Order, (state.Cursor + 1) % state.Order.Length);
        return result; // Advance even when construction or duplicate rejection follows.
    }

    internal TowerAdaptiveBatch Generate(int wave, IReadOnlyList<PartyChoice> beam, IReadOnlySet<string> seenBefore,
        IReadOnlyList<TowerPanelEvaluation> feedback, CancellationToken token)
    {
        if (loadoutPlacement is not null) return loadoutPlacement.Generate(wave, beam, seenBefore, feedback, token);
        if (wave is not (1 or 2)) throw new InvalidDataException("Unknown proposal wave.");
        var schedule = wave == 1 ? policy.FirstWave : policy.SecondWave;
        var seen = seenBefore.ToHashSet(StringComparer.Ordinal);
        var candidates = new List<PartyChoice>();
        var proposals = new List<TowerAdaptiveProposal>();
        for (var attempt = 1; attempt <= TowerAdaptiveRacing.MaximumAttemptsPerWave && candidates.Count < schedule.Count; attempt++)
        {
            if (token.IsCancellationRequested) break;
            var operation = schedule[(attempt - 1) % schedule.Count];
            var waveKey = wave.ToString(CultureInfo.InvariantCulture);
            var attemptKey = attempt.ToString(CultureInfo.InvariantCulture);
            var parents = Stream("parents", waveKey, attemptKey);
            PartyChoice? parent = null, donor = null;
            var source = "fresh";
            if (operation != "fresh")
            {
                var ticket = policy.ParentTickets[parents.Next(policy.ParentTickets.Count)];
                if (ticket == "benchmark") { parent = benchmark; source = "benchmark"; }
                else
                {
                    var choices = ticket == "other-reference" ? references.Where(p => p.Id != benchmark.Id).ToArray()
                        : beam.Count == 0 ? references : beam.ToArray();
                    parent = choices[parents.Next(choices.Length)];
                    source = ticket == "other-reference" ? "other-reference" : beam.Count == 0 ? "reference-fallback" : "beam";
                }
                if (operation == "recombine")
                {
                    var donors = references.Concat(beam).Where(p => p.Id != parent.Id).DistinctBy(p => p.Id).ToArray();
                    donor = donors[Stream("donor", waveKey, attemptKey).Next(donors.Length)];
                }
            }
            var choicesRandom = Stream("choices", waveKey, attemptKey);
            var count = operation is "fresh" or "recombine" ? input.RequiredPartySize
                : operation == "coordinated" ? choicesRandom.Next(2, Math.Min(3, input.RequiredPartySize) + 1) : 1;
            var owners = Owners(operation, parent?.Id ?? "fresh", count);
            var built = Construct(operation, parent, donor, owners, choicesRandom, token);
            var rejection = built.Rejection;
            if (rejection is null && !seen.Add(built.Party!.Id)) rejection = "duplicate-recipe";
            if (rejection is null) candidates.Add(built.Party!);
            var changed = built.Party is null ? [] : built.Party.Builds.Where(p => parent is null
                || !p.Value.SequenceEqual(parent.Builds[p.Key])).Select(p => p.Key).Order().ToArray();
            proposals.Add(new(attempt, operation, built.Effective, source,
                parent is null ? [] : donor is null ? [parent.Id] : [parent.Id, donor.Id], owners, changed, built.Checks,
                built.Party is null || parent is null ? 0 : TowerSuppliedCompositionSearch.Distance(parent, built.Party) / 2,
                built.Interaction, built.Fallback, built.Party, rejection, built.AffinityCreation));
        }
        return new(wave, feedback.Sum(p => p.Observations.Count), feedback.Select(p => HarnessJson.Hash(p.Freeze)).ToArray(),
            beam.Select(p => p.Id).ToArray(), seenBefore.Order(StringComparer.Ordinal).ToArray(), proposals, candidates);
    }

    private sealed record Construction(PartyChoice? Party, int Checks, string Effective,
        string? Interaction, string? Fallback, string? Rejection, TowerAffinityCreationStep? AffinityCreation = null);

    internal TowerAffinityCreationCoverage? CreationCoverage(TowerAdaptiveBatch batch, CancellationToken token)
    {
        var parents = references.Where(p => policy.ParentTickets.Contains("beam")
            || p.Id == benchmark.Id && policy.ParentTickets.Contains("benchmark")
            || p.Id != benchmark.Id && policy.ParentTickets.Contains("other-reference"));
        return affinityCreation?.Coverage(parents, batch, token);
    }

    private Construction Construct(string operation, PartyChoice? parent, PartyChoice? donor, int[] owners,
        Random random, CancellationToken token)
    {
        if (operation == "affinity-create")
        {
            var created = affinityCreation!.Create(parent!, owners[0], random, token);
            return new(created.Party is null ? null : created.Party with { Source = sourceVersion },
                created.Party is null ? 0 : 1, operation, null, null, created.Rejection, created.Step);
        }
        var effective = operation;
        string? fallback = null;
        var differing = donor is null ? [] : owners.Where(owner => !parent!.Builds[owner].SequenceEqual(donor.Builds[owner])).ToArray();
        if (operation == "recombine" && differing.Length < 2)
        {
            effective = "partial";
            fallback = "fewer-than-two-differing-owners";
        }
        for (var check = 1; check <= TowerAdaptiveRacing.MaximumConstructionChecks; check++)
        {
            if (token.IsCancellationRequested) return new(null, check - 1, effective, null, fallback, "cancelled");
            var builds = parent?.Builds.ToDictionary(p => p.Key, p => p.Value.ToArray()) ?? new Dictionary<int, string[]>();
            string? interaction = null;
            var expectedDistance = 0;
            if (effective == "recombine")
            {
                // Same destination owner keeps its actor, gear and subgroup. A proper
                // subset of differing owners guarantees contributions from both parents.
                var take = random.Next(1, differing.Length);
                var offset = random.Next(differing.Length);
                foreach (var owner in Enumerable.Range(0, take).Select(i => differing[(offset + i) % differing.Length]))
                    builds[owner] = donor!.Builds[owner].ToArray();
            }
            else if (effective == "fresh")
            {
                foreach (var owner in owners) builds[owner] = [];
                var complete = true;
                foreach (var owner in owners)
                    if (!Fill(builds, owner, [], random)) { complete = false; break; }
                if (!complete) continue;
            }
            else
            {
                var changed = effective == "coordinated" ? owners : [owners[0]];
                var removed = effective == "partial" ? input.Budget.EssenceSlots -
                    (input.Budget.EssenceSlots == 5 ? random.Next(2, 4) : Math.Max(1, input.Budget.EssenceSlots / 2))
                    : effective == "guided-pair" ? 2 : 1;
                expectedDistance = changed.Length * removed;
                // Free every affected copy before filling any destination.
                var removable = true;
                foreach (var owner in changed)
                {
                    var old = builds[owner].ToArray(); random.Shuffle(old);
                    if (!Preserve) builds[owner] = old.Skip(removed).ToArray();
                    else
                    {
                        var protectedIds = Protected(parent!, owner);
                        var drop = old.Where(id => !protectedIds.Contains(id)).Take(removed).ToHashSet();
                        if (drop.Count != removed) { removable = false; break; }
                        builds[owner] = old.Where(id => !drop.Contains(id)).ToArray();
                    }
                }
                if (!removable) continue;
                if (effective == "guided-pair")
                {
                    var owner = owners[0];
                    var pairs = interactions.Where(p => !parent!.Builds[owner].Contains(p.EnablerEssenceId)
                        && !parent.Builds[owner].Contains(p.ConsumerEssenceId)
                        && CanAdd(builds, owner, p.EnablerEssenceId)
                        && CanAddPair(builds, owner, p.EnablerEssenceId, p.ConsumerEssenceId)).ToArray();
                    if (pairs.Length != 0)
                    {
                        var pair = pairs[random.Next(pairs.Length)];
                        builds[owner] = builds[owner].Concat([pair.EnablerEssenceId, pair.ConsumerEssenceId]).ToArray();
                        interaction = HarnessJson.Hash(pair);
                    }
                    else
                    {
                        // Keep the same two freed assignments; record that this attempt
                        // used an unguided same-owner pair instead of a mechanic hypothesis.
                        fallback = "no-legal-interaction-pair";
                    }
                }
                var complete = true;
                foreach (var owner in changed)
                    if (!Fill(builds, owner, parent!.Builds[owner], random)) { complete = false; break; }
                if (!complete) continue;
            }
            if (Preserve && parent is not null
                && builds.Any(b => !Protected(parent, b.Key).IsSubsetOf(b.Value))) continue;
            var choice = TowerSuppliedCompositionSearch.Choice(input, builds, effective);
            if (choice.Rejection is not null || choice.Party!.Id == parent?.Id || choice.Party.Id == donor?.Id
                || expectedDistance != 0 && TowerSuppliedCompositionSearch.Distance(parent!, choice.Party) != expectedDistance * 2)
                continue;
            return new(choice.Party with { Source = sourceVersion }, check,
                effective == "guided-pair" && interaction is null ? "unguided-pair" : effective,
                interaction, interaction is null ? fallback : null, null);
        }
        return new(null, TowerAdaptiveRacing.MaximumConstructionChecks, effective, null, fallback, "construction-exhausted");
    }

    // Preserve only co-located, declared same-owner hypotheses already present in
    // the parent. This is structural preservation, not a claim of combat synergy.
    private HashSet<string> Protected(PartyChoice parent, int owner)
    {
        if (protectedAssignments.TryGetValue((parent.Id, owner), out var cached)) return cached;
        var ids = protectedInteractions
        .Where(p => policy.PreserveParentInteractions && parent.Builds[owner].Contains(p.EnablerEssenceId) && parent.Builds[owner].Contains(p.ConsumerEssenceId))
        .SelectMany(p => new[] { p.EnablerEssenceId, p.ConsumerEssenceId })
        .Concat(protectedAffinities.Where(a => parent.Builds[owner].Contains(a.ProducerEssenceId) && parent.Builds[owner].Contains(a.ModifierEssenceId))
            .SelectMany(a => new[] { a.ProducerEssenceId, a.ModifierEssenceId })).ToHashSet(StringComparer.Ordinal);
        protectedAssignments.Add((parent.Id, owner), ids);
        return ids;
    }

    private bool CanAdd(Dictionary<int, string[]> builds, int owner, string id) =>
        !builds[owner].Any(old => StringComparer.OrdinalIgnoreCase.Equals(families[old], families[id]))
        && (input.OwnedCopies is null || builds.Values.Sum(ids => ids.Count(old => old == id)) < input.OwnedCopies.GetValueOrDefault(id));

    private bool CanAddPair(Dictionary<int, string[]> builds, int owner, string first, string second)
    {
        var before = builds[owner];
        builds[owner] = before.Append(first).ToArray();
        var result = CanAdd(builds, owner, second);
        builds[owner] = before;
        return result;
    }

    private bool Fill(Dictionary<int, string[]> builds, int owner, IReadOnlyList<string> excluded, Random random)
    {
        while (builds[owner].Length < input.Budget.EssenceSlots)
        {
            var legal = pool.Where(id => !excluded.Contains(id) && CanAdd(builds, owner, id)).ToArray();
            if (legal.Length == 0) return false;
            builds[owner] = builds[owner].Append(legal[random.Next(legal.Length)]).ToArray();
        }
        return true;
    }
}
