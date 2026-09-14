using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerRescreenCandidate(string Id, string PartyId, string ProposalId, int OriginalRank, TowerScenario Scenario);
public sealed record TowerRescreenArm(int Seed, IReadOnlyList<TowerRescreenCandidate> Candidates);
public sealed record TowerRescreenShortlist(int SchemaVersion, string Policy, string DefinitionHash, string DiscoveryHash,
    IReadOnlyList<TowerSearchArm> OriginalArms, IReadOnlyList<TowerRescreenArm> Arms);
public sealed record TowerRescreenEvidence(int Seed, IReadOnlyList<TowerBalanceEvidence> Cells);
public sealed record TowerRescreenNomination(int Seed, string Primary, string Secondary, string EvidenceHash);
public sealed record TowerRescreenSelection(string ShortlistHash, IReadOnlyList<TowerRescreenNomination> Arms);
public sealed record TowerRescreenComparison(string Status, IReadOnlyList<TowerSearchArm> OriginalArms,
    IReadOnlyList<TowerRescreenNomination> RescreenedArms, IReadOnlyList<TowerSearchSelected> Family, string AnchorId);
public sealed record TowerRescreenPrimary(int Seed, string OriginalId, string SelectedId, TowerSearchPrimary Reliability,
    TowerSearchBenchmarkPair SelectionBenefit, bool BenefitSupported);
public sealed record TowerRescreenQuality(string Reliability, string SelectionBenefit, string Adoption,
    IReadOnlyList<TowerRescreenPrimary> Primaries, IReadOnlyDictionary<string, RateEstimate> Rates,
    string JointFamilyAssessment, string Scope);

/// <summary>Opt-in final selection only. Discovery rank, generation and historical nominations keep their meaning.</summary>
public static class TowerFinalistRescreen
{
    public const string Policy = "tower-finalist-rescreen-v1";
    public const int Width = 32, Samples = 64, ConfirmationSamples = 512, FamilyCapacity = 64;
    public const int DiscoveryFights = 18432, RescreenFights = 6144, MaximumFights = 57344;
    public const string CandidateMethod = "loadout-composition-joint";

    public static void Validate(TowerBossDiscoveryDefinition d)
    {
        TowerBossDiscovery.Validate(d);
        if (d.Mode != TowerBossDiscovery.Independent || d.References.Count != 0 || d.Starts.Count != 0
            || d.Generation.PolicyVersion != TowerBossGeneration.LoadoutCompositionVersion
            || !d.Generation.Methods.SequenceEqual(TowerBossGeneration.LoadoutCompositionMethods)
            || d.Generation.CandidatesPerArm != 384 || d.Generation.Seeds.Count != 3
            || d.Generation.MaximumAttemptsPerArm != 8192 || d.Generation.FreshEvery != 4
            || d.Contexts.Count != 1 || d.Stages.Shortlist != 12 || d.Stages.DiagnosticCandidates != 0 || d.Stages.ReplayReserve != 0
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != 8 || s.Selection.Count != Samples
                || s.Confirmation.Count != ConfirmationSamples || s.Diagnostics.Count != 0)
            || d.Generation.Seeds.Intersect(d.ExcludedCombatSeeds.Concat(d.Stages.Schedules.Values.SelectMany(s =>
                s.Discovery.Concat(s.Selection).Concat(s.Confirmation)))).Any())
            throw new InvalidDataException("Rescreen requires reference-free unchanged v13 discovery, three 384-candidate paired restarts and disjoint 8/64/512 schedules.");
    }

    public static TowerRescreenShortlist Freeze(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery)
    {
        Validate(d);
        if (discovery is null || discovery.Status != "Complete" || discovery.Error is not null
            || discovery.PlannedDiscoveryBattles != DiscoveryFights || discovery.ActualBattles != DiscoveryFights || discovery.CacheHits != 0
            || discovery.Generation is not { Status: "Complete", Error: null } g || g.Version != d.Generation.PolicyVersion
            || g.Arms.Count != 6 || !g.Arms.Select(a => (a.Method, a.Seed)).SequenceEqual(
                d.Generation.Seeds.SelectMany(seed => d.Generation.Methods.Select(method => (method, seed)))))
            throw new InvalidDataException("A complete declared discovery matrix is required before freezing a rescreen.");
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        var originals = new List<TowerSearchArm>(); var arms = new List<TowerRescreenArm>();
        foreach (var arm in g.Arms)
        {
            if (arm.StopReason != "CandidateBudgetReached" || arm.Evaluations.Count != 384 || arm.Proposals.Count > 8192
                || arm.Evaluations.Select(e => e.Id).Distinct().Count() != 384)
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
            if (measured.Length != 384 || measured.Any(p => p.Party is null) || measured.Select(p => p.Party!.Id).Distinct().Count() != 384)
                throw new InvalidDataException("Evaluations must match unique completed proposals.");
            var proposals = measured.ToDictionary(p => p.Party!.Id);
            foreach (var row in arm.Evaluations)
            {
                if (!proposals.TryGetValue(row.Id, out var proposal)
                    || row.Fitness != TowerBossGeneration.Fitness(inputs, row.Cells, row.Fitness.VictoryDuration))
                    throw new InvalidDataException("Discovery measurements or fitness differ from the complete matrix.");
                TowerBossDiscovery.ValidateParty(d, proposal.Party!);
            }
            var ranked = TowerBossGeneration.Rank(arm.Evaluations).Select((row, i) => {
                var p = proposals[row.Id]; var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
                return new TowerRescreenCandidate(Id(scenario), row.Id, p.Provenance.Id, i + 1, scenario);
            }).ToArray();
            if (ranked.Select(r => r.Id).Distinct().Count() != 384) throw new InvalidDataException("Duplicate normalized candidate recipes.");
            originals.Add(new(arm.Method, arm.Seed, ranked[0].Id, ranked[1].Id));
            if (arm.Method == CandidateMethod) arms.Add(new(arm.Seed, ranked.Take(Width).ToArray()));
        }
        // Detach caller-owned recipe vectors and templates at the frozen selection boundary.
        return JsonSerializer.Deserialize<TowerRescreenShortlist>(JsonSerializer.Serialize(new TowerRescreenShortlist(
            1, Policy, HarnessJson.Hash(d), HarnessJson.Hash(discovery), originals, arms), HarnessJson.Options), HarnessJson.Options)!;
    }

    internal static string Id(TowerScenario scenario) => "team-" + TowerBossDiscovery.RecipeHash(scenario.Party)[..32];

    public static TowerBalanceDefinition RescreenDefinition(TowerBossDiscoveryDefinition d, TowerRescreenArm arm) =>
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
                .Concat(confirmation ? schedule.Selection : schedule.Confirmation).Distinct().Order().ToArray(), family.Count * seeds.Count);
        TowerBalanceEvaluator.Validate(definition); return definition;
    }

    internal static void RequireEvidence(TowerBalanceDefinition d, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        var report = TowerBalanceEvaluator.Evaluate(d, evidence);
        if (report.Issues.Count != 0 || report.Cells.Any(c => c.Issues.Count != 0)
            || evidence.Count != d.Cells.Count || evidence.Any(e => e.Trials.Count != d.Cells[0].MinimumSamples))
            throw new InvalidDataException("Selection requires every exact frozen recipe and complete independent seed matrix.");
    }

    public static TowerRescreenSelection Select(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery,
        TowerRescreenShortlist shortlist, IReadOnlyList<TowerRescreenEvidence> evidence)
    {
        if (HarnessJson.Hash(shortlist) != HarnessJson.Hash(Freeze(d, discovery)) || evidence.Count != 3
            || !evidence.Select(e => e.Seed).Order().SequenceEqual(shortlist.Arms.Select(a => a.Seed).Order()))
            throw new InvalidDataException("Frozen shortlist or declared rescreen arms differ.");
        var selected = new List<TowerRescreenNomination>();
        foreach (var arm in shortlist.Arms)
        {
            var rows = evidence.Single(e => e.Seed == arm.Seed).Cells;
            RequireEvidence(RescreenDefinition(d, arm), rows);
            var counts = rows.ToDictionary(e => e.CellId, e => e.Trials.Count(t => t.Outcome == BattleOutcome.Victory));
            var ranked = arm.Candidates.OrderByDescending(c => counts[c.Id]).ThenBy(c => c.OriginalRank).ToArray();
            selected.Add(new(arm.Seed, ranked[0].Id, ranked[1].Id, HarnessJson.Hash(rows.OrderBy(e => e.CellId, StringComparer.Ordinal).ToArray())));
        }
        return new(HarnessJson.Hash(shortlist), selected);
    }

    public static TowerRescreenComparison Compare(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery,
        TowerRescreenShortlist shortlist, TowerRescreenSelection selected, IReadOnlyList<TowerRescreenEvidence> evidence,
        IReadOnlyList<TowerSearchSelected> controls, string anchorId)
    {
        if (HarnessJson.Hash(selected) != HarnessJson.Hash(Select(d, discovery, shortlist, evidence)))
            throw new InvalidDataException("Rescreen nominations changed after selection.");
        ValidateControls(d, controls, anchorId);
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
        foreach (var arm in discovery.Generation!.Arms)
        {
            var proposals = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            foreach (var (row, index) in TowerBossGeneration.Rank(arm.Evaluations).Select((row, i) => (row, i)))
                if (index < 2 || row.Cells.Single().Clears.Count(w => w) > 4)
                    Add(TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, proposals[row.Id].Party!, []),
                        new(arm.Method, arm.Seed, index + 1, row.Id, null));
        }
        foreach (var arm in shortlist.Arms)
        {
            var nomination = selected.Arms.Single(a => a.Seed == arm.Seed);
            var counts = evidence.Single(e => e.Seed == arm.Seed).Cells.ToDictionary(e => e.CellId, e => e.Trials.Count(t => t.Outcome == BattleOutcome.Victory));
            foreach (var c in arm.Candidates)
                if (c.Id == nomination.Primary || c.Id == nomination.Secondary || counts[c.Id] > Samples / 2)
                    Add(c.Scenario, new(CandidateMethod + "-rescreen", arm.Seed, c.OriginalRank, c.PartyId, null));
        }
        return new(family.Count > FamilyCapacity ? "CapacityExceeded" : "Ready", shortlist.OriginalArms, selected.Arms,
            family.Values.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray(), anchorId);
    }

    public static void ValidateControls(TowerBossDiscoveryDefinition d, IReadOnlyList<TowerSearchSelected> controls, string anchorId)
    {
        if (controls.Count != 20 || controls.Select(c => c.Id).Distinct().Count() != 20 || controls.All(c => c.Id != anchorId)
            || controls.Any(c => c.Id != Id(c.Scenario) || c.Scenario.Seeds.Count != 0 || c.Scenario.StartsAt != d.StartsAt
                || c.Scenario.FloorNumber != d.Budget.PriorityFloor))
            throw new InvalidDataException("The separately registered twenty complete controls and fixed anchor are required.");
        TowerBossDiscovery.Validate(d with { References = controls.Select(c => new BossBenchmarkReference(c.Id,
            d.Contexts[0].Id, c.Scenario, "Explicit external control", HarnessJson.Hash(c))).ToArray() });
    }

    public static TowerBalanceDefinition ConfirmationDefinition(TowerBossDiscoveryDefinition d, TowerRescreenComparison comparison)
    {
        if (comparison.Status != "Ready" || comparison.Family.Count > FamilyCapacity)
            throw new InvalidDataException("An overflowing family must remain preserved without confirmation.");
        return Balance(d, comparison.Family, true);
    }

    public static TowerRescreenQuality Quality(TowerBossDiscoveryDefinition d, TowerRescreenComparison comparison,
        IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        RequireEvidence(ConfirmationDefinition(d, comparison), evidence);
        if (comparison.OriginalArms.Count != 6 || comparison.RescreenedArms.Count != 3
            || !comparison.RescreenedArms.Select(a => a.Seed).SequenceEqual(d.Generation.Seeds)
            || !comparison.OriginalArms.Select(a => (a.Method, a.Seed)).SequenceEqual(
                d.Generation.Seeds.SelectMany(seed => d.Generation.Methods.Select(method => (method, seed)))))
            throw new InvalidDataException("The complete original and rescreened nomination matrix is required.");
        var byId = evidence.ToDictionary(e => e.CellId);
        var rates = evidence.ToDictionary(e => e.CellId, e => TowerBalanceEvaluator.Wilson(
            e.Trials.Count(t => t.Outcome == BattleOutcome.Victory), ConfirmationSamples, 2 * evidence.Count)!);
        var primaries = comparison.RescreenedArms.Select(a => {
            var original = comparison.OriginalArms.Single(b => b.Seed == a.Seed && b.Method == CandidateMethod);
            var baseline = comparison.OriginalArms.Single(b => b.Seed == a.Seed && b.Method == "coverage-deep-joint");
            var trials = byId[a.Primary].Trials; var rate = rates[a.Primary];
            var comparator = Pair(trials, byId[baseline.Primary].Trials); var anchor = Pair(trials, byId[comparison.AnchorId].Trials);
            var benefit = Pair(trials, byId[original.Primary].Trials);
            var viable = rate.Lower >= .1; var improved = comparator.Lower > 0; var recovered = anchor.Lower >= -.1;
            return new TowerRescreenPrimary(a.Seed, original.Primary, a.Primary, new(CandidateMethod, a.Seed, a.Primary,
                trials.Count(t => t.Outcome == BattleOutcome.Victory), rate, comparator, anchor, viable, improved, recovered,
                viable && improved && recovered), benefit, benefit.Lower > 0);
        }).ToArray();
        var reliable = primaries.Count(p => p.Reliability.Pass) >= 2; var beneficial = primaries.Count(p => p.BenefitSupported) >= 2;
        var family = rates.Values.Any(r => r.Rate > .5) || rates.Values.All(r => r.Upper < .1) ? "Fail"
            : rates.Values.All(r => r.Upper <= .5) && rates.Values.Any(r => r.Lower >= .1) ? "Pass" : "Inconclusive";
        return new(reliable ? "Pass" : "Fail", beneficial ? "Pass" : "Fail", reliable && beneficial ? "Eligible" : "Hold",
            primaries, rates, family, "Joint alpha .025 across all confirmation rates and .025 across nine paired differences (18 discordance intervals). "
            + "512 fresh shared trials per recipe; no pooling. Reliability and selection benefit each require 2/3 restarts. "
            + "Approximate Wilson coverage; no lifetime repeated-study, full generated-family, near-optimality or acquisition claim.");
    }

    internal static TowerSearchBenchmarkPair Pair(IReadOnlyList<TowerBalanceTrial> left, IReadOnlyList<TowerBalanceTrial> right)
    {
        if (left.Count != ConfirmationSamples || !left.Select(t => t.Seed).SequenceEqual(right.Select(t => t.Seed))
            || left.Select(t => t.Seed).Distinct().Count() != left.Count || left.Concat(right).Any(t => !Enum.IsDefined(t.Outcome)))
            throw new InvalidDataException("Rescreen comparison requires 512 complete paired trials.");
        var gains = left.Zip(right).Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
        var losses = left.Zip(right).Count(p => p.Second.Outcome == BattleOutcome.Victory && p.First.Outcome != BattleOutcome.Victory);
        var g = TowerBalanceEvaluator.Wilson(gains, left.Count, 36)!; var l = TowerBalanceEvaluator.Wilson(losses, left.Count, 36)!;
        return new(left.Count, gains, losses, (gains - losses) / (double)left.Count, g.Lower - l.Upper, g.Upper - l.Lower);
    }
}
