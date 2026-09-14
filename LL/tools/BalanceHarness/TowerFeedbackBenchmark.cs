using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerFeedbackArm(string Method, int Seed, IReadOnlyList<TowerRescreenCandidate> Candidates);
public sealed record TowerFeedbackShortlist(int SchemaVersion, string Policy, string DefinitionHash, string DiscoveryHash,
    IReadOnlyList<TowerSearchArm> OriginalArms, IReadOnlyList<TowerFeedbackArm> Arms,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerAllocationUnit>? Allocations = null);
public sealed record TowerFeedbackEvidence(string Method, int Seed, IReadOnlyList<TowerBalanceEvidence> Cells);
public sealed record TowerFeedbackNomination(string Method, int Seed, string Primary, string Secondary, string EvidenceHash);
public sealed record TowerFeedbackSelection(string ShortlistHash, IReadOnlyList<TowerFeedbackNomination> Arms);
public sealed record TowerFeedbackComparison(string Status, IReadOnlyList<TowerSearchArm> OriginalArms,
    IReadOnlyList<TowerFeedbackNomination> RescreenedArms, IReadOnlyList<TowerSearchSelected> Family, string AnchorId, string StrongControlId);
public sealed record TowerFeedbackPrimary(int Seed, string SelectedId, TowerSearchPrimary Reliability, TowerSearchBenchmarkPair StrongControl);
public sealed record TowerFeedbackQuality(string Reliability, string Adoption,
    IReadOnlyList<TowerFeedbackPrimary> Primaries, IReadOnlyDictionary<string, RateEstimate> Rates,
    string JointFamilyAssessment, string Scope);

/// <summary>Fixed-budget generation feedback, with identical independent final selection for both methods.</summary>
public static class TowerFeedbackBenchmark
{
    public const string Policy = "tower-generation-feedback-v1";
    public const int Width = 32, Samples = 64, ConfirmationSamples = 512, FamilyCapacity = 64;
    public const int DiscoveryFights = 18432, RescreenFights = 12288, MaximumFights = 63488;
    public const string CandidateMethod = TowerGenerationFeedback.Method;

    public static void Validate(TowerBossDiscoveryDefinition d)
    {
        TowerBossDiscovery.Validate(d);
        var design = TowerGenerationComparisonDesign.FromDefinition(d);
        if (d.Mode != TowerBossDiscovery.Independent || d.References.Count != 0 || d.Starts.Count != 0
            || d.Generation.PolicyVersion != design.GenerationVersion
            || !d.Generation.Methods.SequenceEqual(design.Methods)
            || d.Generation.CandidatesPerArm != design.CandidatesPerArm || d.Generation.Seeds.Count != 3
            || d.Generation.MaximumAttemptsPerArm != design.MaximumAttempts || d.Generation.FreshEvery != 4
            || d.Contexts.Count != 1 || d.Stages.Shortlist != 12 || d.Stages.GeneratedFinalists != 5
            || d.Stages.DiagnosticCandidates != 0 || d.Stages.ReplayReserve != 0 || d.MaximumBattles != design.MaximumFights
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != 8 || s.Selection.Count != Samples
                || s.Confirmation.Count != ConfirmationSamples || s.Diagnostics.Count != 0 || (design.FeedbackSamples == 0 ? s.Feedback is not null : s.Feedback?.Count != design.FeedbackSamples))
            || d.Generation.Seeds.Intersect(d.ExcludedCombatSeeds.Concat(d.Stages.Schedules.Values.SelectMany(s =>
                s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Feedback ?? [])))).Any())
            throw new InvalidDataException("Generation comparison requires its reference-free paired policy, fixed candidate budget and disjoint complete schedules.");
    }

    public static TowerFeedbackShortlist Freeze(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery)
    {
        Validate(d);
        var design = TowerGenerationComparisonDesign.FromDefinition(d);
        if (discovery is null || discovery.Status != "Complete" || discovery.Error is not null
            || discovery.PlannedDiscoveryBattles != design.DiscoveryFights || discovery.ActualBattles != design.DiscoveryFights || discovery.CacheHits != 0
            || discovery.Generation is not { Status: "Complete", Error: null } g || g.Version != d.Generation.PolicyVersion
            || g.Arms.Count != design.Methods.Length * 3 || !g.Arms.Select(a => (a.Method, a.Seed)).SequenceEqual(
                d.Generation.Seeds.SelectMany(seed => d.Generation.Methods.Select(method => (method, seed)))))
            throw new InvalidDataException("A complete declared discovery matrix is required before freezing a rescreen.");
        TowerLateAllocation.ValidateComplete(d.Generation, g);
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        var originals = new List<TowerSearchArm>(); var arms = new List<TowerFeedbackArm>();
        foreach (var arm in g.Arms)
        {
            var budget = d.Generation.PolicyVersion is TowerLateAllocation.Version or TowerSearchPortfolio.Version ? TowerLateAllocation.FinalBudget(g, arm) : TowerBossGeneration.CandidateBudget(d.Generation, arm.Method);
            TowerGenerationFeedback.ValidateRounds(inputs, arm);
            TowerPartyLineages.ValidateArm(arm);
            if (arm.StopReason != "CandidateBudgetReached" || arm.Evaluations.Count != budget || arm.Proposals.Count > TowerBossGeneration.AttemptBudget(d.Generation, arm.Method)
                || arm.Evaluations.Select(e => e.Id).Distinct().Count() != budget)
                throw new InvalidDataException("An incomplete or duplicate arm cannot freeze finalists.");
            TowerBossDiscovery.ValidateProvenance(d, arm.Proposals.Select(p => p.Provenance).ToArray());
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in arm.Proposals)
            {
                if (p.Provenance.Method != arm.Method || p.Provenance.GenerationSeed != arm.Seed
                    || p.Provenance.ReferenceIds.Count != 0 || p.Provenance.ParentIds.Any(id => !parents.Contains(id)))
                    throw new InvalidDataException("Finalists require completed same-arm independent ancestry.");
                if (p.Result == "evaluated") parents.Add(p.Provenance.Id);
            }
            var measured = arm.Proposals.Where(p => p.Result == "evaluated").ToArray();
            if (measured.Length != budget || measured.Any(p => p.Party is null) || measured.Select(p => p.Party!.Id).Distinct().Count() != budget)
                throw new InvalidDataException("Evaluations must match unique completed proposals.");
            var proposals = measured.ToDictionary(p => p.Party!.Id);
            foreach (var row in arm.Evaluations)
            {
                if (!proposals.TryGetValue(row.Id, out var proposal)
                    || row.Fitness != TowerBossGeneration.Fitness(inputs, row.Cells, row.Fitness.VictoryDuration))
                    throw new InvalidDataException("Discovery measurements or fitness differ from the complete matrix.");
                TowerBossDiscovery.ValidateParty(d, proposal.Party!);
            }
            if (design.IsAllocation) continue;
            var ranked = TowerBossGeneration.Rank(TowerGenerationFeedback.Effective(inputs, arm.Evaluations, arm.Feedback)).Select((row, i) => {
                var p = proposals[row.Id]; var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
                return new TowerRescreenCandidate(Id(scenario), row.Id, p.Provenance.Id, i + 1, scenario);
            }).ToArray();
            if (ranked.Select(r => r.Id).Distinct().Count() != budget) throw new InvalidDataException("Duplicate normalized candidate recipes.");
            originals.Add(new(arm.Method, arm.Seed, ranked[0].Id, ranked[1].Id));
            arms.Add(new(arm.Method, arm.Seed, ranked.Take(Width).ToArray()));
        }
        if (design.IsAllocation) return TowerSearchAllocation.Freeze(d, discovery);
        // Detach caller-owned recipe vectors and templates at the frozen selection boundary.
        return JsonSerializer.Deserialize<TowerFeedbackShortlist>(JsonSerializer.Serialize(new TowerFeedbackShortlist(
            1, TowerGenerationComparisonDesign.FromDefinition(d).Policy, HarnessJson.Hash(d), HarnessJson.Hash(discovery), originals, arms), HarnessJson.Options), HarnessJson.Options)!;
    }

    internal static string Id(TowerScenario scenario) => "team-" + TowerBossDiscovery.RecipeHash(scenario.Party)[..32];

    public static TowerBalanceDefinition RescreenDefinition(TowerBossDiscoveryDefinition d, TowerFeedbackArm arm) =>
        Balance(d, arm.Candidates.Select(c => new TowerSearchSelected(c.Id, c.Scenario, [])).ToArray(), false);

    internal static TowerBalanceDefinition Balance(TowerBossDiscoveryDefinition d, IReadOnlyList<TowerSearchSelected> family, bool confirmation)
    {
        Validate(d);
        var schedule = d.Stages.Schedules.Values.Single(); var seeds = confirmation ? schedule.Confirmation : schedule.Selection;
        var context = d.Contexts[0];
        var cohort = new TowerBalanceCohort("fixed-cohort", d.Budget, d.RequiredPartySize, context.Id,
            TowerBossDiscovery.EquipmentBudgetHash(context.CharacterTemplates), d.BudgetPurpose);
        var definition = new TowerBalanceDefinition(1, confirmation ? "rescreen-confirmation" : "finalist-rescreen", TowerBalanceEvaluator.IntervalPolicy,
            d.ContentHashes, d.SettingsHash, d.ExecutionHash, [cohort], family.Select(f => new TowerBalanceCellDefinition(f.Id, cohort.Id,
                f.Sources.Any(s => s.Method == "saved-control") ? "reference" : "generated", f.Scenario with { Seeds = seeds }, seeds.Count)).ToArray(),
            d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(schedule.Discovery)
                .Concat(schedule.Feedback ?? []).Concat(confirmation ? schedule.Selection : schedule.Confirmation).Distinct().Order().ToArray(), family.Count * seeds.Count);
        TowerBalanceEvaluator.Validate(definition); return definition;
    }

    internal static void RequireEvidence(TowerBalanceDefinition d, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        var report = TowerBalanceEvaluator.Evaluate(d, evidence);
        if (report.Issues.Count != 0 || report.Cells.Any(c => c.Issues.Count != 0)
            || evidence.Count != d.Cells.Count || evidence.Any(e => e.Trials.Count != d.Cells[0].MinimumSamples))
            throw new InvalidDataException("Selection requires every exact frozen recipe and complete independent seed matrix.");
    }

    public static TowerFeedbackSelection Select(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery,
        TowerFeedbackShortlist shortlist, IReadOnlyList<TowerFeedbackEvidence> evidence)
    {
        if (HarnessJson.Hash(shortlist) != HarnessJson.Hash(Freeze(d, discovery)) || evidence.Count != 6
            || !evidence.Select(e => (e.Method, e.Seed)).Order().SequenceEqual(shortlist.Arms.Select(a => (a.Method, a.Seed)).Order()))
            throw new InvalidDataException("Frozen shortlist or declared rescreen arms differ.");
        var selected = new List<TowerFeedbackNomination>();
        foreach (var arm in shortlist.Arms)
        {
            var rows = evidence.Single(e => e.Seed == arm.Seed && e.Method == arm.Method).Cells;
            RequireEvidence(RescreenDefinition(d, arm), rows);
            var counts = rows.ToDictionary(e => e.CellId, e => e.Trials.Count(t => t.Outcome == BattleOutcome.Victory));
            var ranked = arm.Candidates.OrderByDescending(c => counts[c.Id]).ThenBy(c => c.OriginalRank).ToArray();
            selected.Add(new(arm.Method, arm.Seed, ranked[0].Id, ranked[1].Id, HarnessJson.Hash(rows.OrderBy(e => e.CellId, StringComparer.Ordinal).ToArray())));
        }
        return new(HarnessJson.Hash(shortlist), selected);
    }

    public static TowerFeedbackComparison Compare(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery,
        TowerFeedbackShortlist shortlist, TowerFeedbackSelection selected, IReadOnlyList<TowerFeedbackEvidence> evidence,
        IReadOnlyList<TowerSearchSelected> controls, string anchorId, string strongControlId)
    {
        if (HarnessJson.Hash(selected) != HarnessJson.Hash(Select(d, discovery, shortlist, evidence)))
            throw new InvalidDataException("Rescreen nominations changed after selection.");
        ValidateControls(d, controls, anchorId, strongControlId);
        var family = new Dictionary<string, TowerSearchSelected>(StringComparer.Ordinal);
        void Add(TowerScenario scenario, TowerSearchOrigin origin)
        {
            var id = Id(scenario);
            if (!family.TryGetValue(id, out var row)) family.Add(id, row = new(id, scenario with { Seeds = [] }, []));
            if (TowerBossDiscovery.RecipeHash(row.Scenario.Party) != TowerBossDiscovery.RecipeHash(scenario.Party))
                throw new InvalidDataException("Recipe identity collision.");
            if (!row.Sources.Contains(origin)) row.Sources.Add(origin);
        }
        foreach (var control in controls) Add(control.Scenario, new("saved-control", null, null, null, control.Id));
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        if (TowerGenerationComparisonDesign.FromDefinition(d).IsAllocation)
        {
            foreach (var seed in d.Generation.Seeds)
            foreach (var method in TowerGenerationComparisonDesign.FromDefinition(d).ComparisonMethods)
                foreach (var row in TowerSearchAllocation.Candidates(d, discovery.Generation!, method, seed))
                    if (row.Candidate.OriginalRank <= 2 || row.Measurement.Fitness.WorstContextWinRate > .5)
                    {
                        Add(row.Candidate.Scenario, new(method, seed, row.Candidate.OriginalRank, row.Candidate.PartyId, null));
                        foreach (var source in row.Sources)
                            Add(row.Candidate.Scenario, new(source.Method + "-component", seed, source.Rank, source.PartyId, null));
                    }
        }
        else foreach (var arm in discovery.Generation!.Arms)
        {
            var proposals = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            foreach (var (row, index) in TowerBossGeneration.Rank(TowerGenerationFeedback.Effective(inputs, arm.Evaluations, arm.Feedback)).Select((row, i) => (row, i)))
                if (index < 2 || arm.Evaluations.Single(e => e.Id == row.Id).Fitness.WorstContextWinRate > .5
                    || (arm.Feedback?.SelectMany(r => r.Measurements).Any(e => e.Id == row.Id && e.Fitness.WorstContextWinRate > .5) ?? false))
                    Add(TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, proposals[row.Id].Party!, []),
                        new(arm.Method, arm.Seed, index + 1, row.Id, null));
        }
        foreach (var arm in shortlist.Arms)
        {
            var nomination = selected.Arms.Single(a => a.Seed == arm.Seed && a.Method == arm.Method);
            var counts = evidence.Single(e => e.Seed == arm.Seed && e.Method == arm.Method).Cells.ToDictionary(e => e.CellId, e => e.Trials.Count(t => t.Outcome == BattleOutcome.Victory));
            foreach (var c in arm.Candidates)
                if (c.Id == nomination.Primary || c.Id == nomination.Secondary || counts[c.Id] > Samples / 2)
                    {
                    Add(c.Scenario, new(arm.Method + "-rescreen", arm.Seed, c.OriginalRank, c.PartyId, null));
                    if (shortlist.Allocations is not null)
                        foreach (var source in shortlist.Allocations.Single(a => a.Method == arm.Method && a.Seed == arm.Seed)
                            .Ranking.Single(r => r.Id == c.Id).Sources)
                            Add(c.Scenario, new(source.Method + "-component", arm.Seed, source.Rank, source.PartyId, null));
                }
        }
        return new(family.Count > TowerGenerationComparisonDesign.FromDefinition(d).FamilyCapacity ? "CapacityExceeded" : "Ready", shortlist.OriginalArms, selected.Arms,
            family.Values.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray(), anchorId, strongControlId);
    }

    public static void ValidateControls(TowerBossDiscoveryDefinition d, IReadOnlyList<TowerSearchSelected> controls, string anchorId, string strongControlId)
    {
        if (controls.Count != TowerGenerationComparisonDesign.FromDefinition(d).Controls || controls.Select(c => c.Id).Distinct().Count() != controls.Count || controls.All(c => c.Id != anchorId) || controls.All(c => c.Id != strongControlId) || anchorId == strongControlId
            || controls.Any(c => c.Id != Id(c.Scenario) || c.Scenario.Seeds.Count != 0 || c.Scenario.StartsAt != d.StartsAt
                || c.Scenario.FloorNumber != d.Budget.PriorityFloor))
            throw new InvalidDataException("The complete separately registered controls, fixed anchor and strongest prior control are required.");
        TowerBossDiscovery.Validate(d with { References = controls.Select(c => new BossBenchmarkReference(c.Id,
            d.Contexts[0].Id, c.Scenario, "Explicit external control", HarnessJson.Hash(c))).ToArray() });
    }

    public static TowerBalanceDefinition ConfirmationDefinition(TowerBossDiscoveryDefinition d, TowerFeedbackComparison comparison)
    {
        if (comparison.Status != "Ready" || comparison.Family.Count > TowerGenerationComparisonDesign.FromDefinition(d).FamilyCapacity)
            throw new InvalidDataException("An overflowing family must remain preserved without confirmation.");
        return Balance(d, comparison.Family, true);
    }

    public static TowerFeedbackQuality Quality(TowerBossDiscoveryDefinition d, TowerFeedbackComparison comparison,
        IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        RequireEvidence(ConfirmationDefinition(d, comparison), evidence);
        if (comparison.OriginalArms.Count != 6 || comparison.RescreenedArms.Count != 6
            || !comparison.RescreenedArms.Select(a => (a.Method, a.Seed)).SequenceEqual(
                d.Generation.Seeds.SelectMany(seed => TowerGenerationComparisonDesign.FromDefinition(d).ComparisonMethods.Select(method => (method, seed))))
            || !comparison.OriginalArms.Select(a => (a.Method, a.Seed)).SequenceEqual(
                d.Generation.Seeds.SelectMany(seed => TowerGenerationComparisonDesign.FromDefinition(d).ComparisonMethods.Select(method => (method, seed)))))
            throw new InvalidDataException("The complete paired nomination matrix is required.");
        return ConfirmationQuality(ConfirmationDefinition(d, comparison), comparison,
            TowerGenerationComparisonDesign.FromDefinition(d).CandidateMethod, evidence);
    }

    // Shared unchanged gate for separately frozen confirmation of saved nominees.
    internal static TowerFeedbackQuality ConfirmationQuality(TowerBalanceDefinition definition, TowerFeedbackComparison comparison,
        string candidateMethod, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        RequireEvidence(definition, evidence);
        var byId = evidence.ToDictionary(e => e.CellId);
        var rates = evidence.ToDictionary(e => e.CellId, e => TowerBalanceEvaluator.Wilson(
            e.Trials.Count(t => t.Outcome == BattleOutcome.Victory), ConfirmationSamples, 2 * evidence.Count)!);
        var primaries = comparison.RescreenedArms.Where(a => a.Method == candidateMethod).Select(a => {
            var baseline = comparison.RescreenedArms.Single(b => b.Seed == a.Seed && b.Method == TowerGenerationFeedback.Methods[0]);
            var trials = byId[a.Primary].Trials; var rate = rates[a.Primary];
            var comparator = Pair(trials, byId[baseline.Primary].Trials); var anchor = Pair(trials, byId[comparison.AnchorId].Trials);
            var strong = Pair(trials, byId[comparison.StrongControlId].Trials);
            var viable = rate.Lower >= .1; var improved = comparator.Lower > 0; var recovered = anchor.Lower >= -.1;
            return new TowerFeedbackPrimary(a.Seed, a.Primary, new(candidateMethod, a.Seed, a.Primary,
                trials.Count(t => t.Outcome == BattleOutcome.Victory), rate, comparator, anchor, viable, improved, recovered,
                viable && improved && recovered), strong);
        }).ToArray();
        var reliable = primaries.Count(p => p.Reliability.Pass) >= 2;
        var family = rates.Values.Any(r => r.Rate > .5) || rates.Values.All(r => r.Upper < .1) ? "Fail"
            : rates.Values.All(r => r.Upper <= .5) && rates.Values.Any(r => r.Lower >= .1) ? "Pass" : "Inconclusive";
        return new(reliable ? "Pass" : "Fail", reliable ? "Eligible" : "Hold", primaries, rates, family,
            "Joint alpha .025 across all confirmation rates and .025 across nine paired differences (18 discordance intervals). "
            + "512 fresh shared trials per recipe; no confirmation pooling. Reliability requires 2/3 restarts. "
            + "Strongest prior control comparison is reported separately and does not change that gate. "
            + "Approximate Wilson coverage; no lifetime repeated-study, full generated-family, near-optimality or acquisition claim.");
    }

    internal static TowerSearchBenchmarkPair Pair(IReadOnlyList<TowerBalanceTrial> left, IReadOnlyList<TowerBalanceTrial> right) =>
        TowerFinalistRescreen.Pair(left, right);
}
