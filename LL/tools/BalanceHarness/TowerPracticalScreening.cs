namespace BalanceHarness;

public sealed record PracticalScreeningFreeze(string Version, string DefinitionHash, string DiscoveryHash,
    int AfterDiscoveryFights, IReadOnlyList<int> Seeds, IReadOnlyList<PartyChoice> Candidates);
public sealed record PracticalScreeningResult(string Version, PracticalScreeningFreeze Freeze,
    IReadOnlyList<BossDiscoveryMeasurement> Measurements, int AfterSearchFights, IReadOnlyList<PartyChoice> Nominees);

/// <summary>Fresh, fixed-width screening between adaptive discovery and final nomination.</summary>
public static class TowerPracticalScreening
{
    public const string Version = "tower-practical-fresh-screening-v1";
    public const string BaselineVersion = "tower-practical-direct-nomination-v1";
    internal const int DiscoveryTrials = 4, ScreeningTrials = 8, Candidates = 23, DiscoveryFights = 184, ScreeningFights = 184;

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidDataException(message); }

    internal static void Validate(TowerBossDiscoveryDefinition d, IReadOnlyList<int> screening)
    {
        var cost = TowerBossDiscovery.Validate(d);
        Require(d.Generation.PolicyVersion == TowerSuppliedCompositionSearch.ThreeReferenceVersion
            && d.Generation.CandidatesPerArm == 46 && d.Generation.MaximumAttemptsPerArm == 256
            && d.Generation.Seeds.Count == 1 && d.Contexts.Count == 1 && d.Starts.Count == 3
            && d.Stages.Shortlist == 5 && d.Stages.GeneratedFinalists == 1
            && d.Stages.SelectionPolicyVersion == TowerBossStudyPolicy.IncumbentTieVersion
            && d.Stages.Schedules.Values.All(s => s.Discovery.Count == DiscoveryTrials && s.Selection.Count == 32)
            && cost.Discovery == DiscoveryFights && cost.Selection == 160,
            "Fresh screening requires the fixed four-trial three-reference discovery and 32-trial selector.");
        Require(screening.Count == ScreeningTrials && screening.Distinct().Count() == ScreeningTrials
            && !screening.Intersect(TowerPracticalSearch.Reserved(d).Concat(d.ExcludedCombatSeeds)).Any(),
            "Screening requires eight fresh values disjoint from every declared stage and construction root.");
    }

    private static void Measurements(BossDiscoveryInputs inputs, IReadOnlyList<PartyChoice> parties,
        IReadOnlyList<BossDiscoveryMeasurement> rows, int samples)
    {
        Require(rows.Count == parties.Count && rows.Select(r => r.Id).Distinct().Count() == rows.Count
            && parties.Select(p => p.Id).Distinct().Count() == parties.Count
            && rows.Select(r => r.Id).Order().SequenceEqual(parties.Select(p => p.Id).Order()),
            "Screening requires every frozen recipe exactly once.");
        foreach (var row in rows)
            Require(row.Cells.Count == 1 && row.Cells[0].Trials.Count == samples
                && row.Cells[0].Trials.Distinct().Count() == samples
                && row.Fitness == TowerBossGeneration.Fitness(inputs, row.Cells, row.Fitness.VictoryDuration),
                "Incomplete or inconsistent screening measurement.");
    }

    internal static PracticalScreeningFreeze Freeze(TowerBossDiscoveryDefinition d, BossGenerationResult discovery,
        IReadOnlyList<int> seeds)
    {
        Validate(d, seeds);
        Require(discovery.Version == d.Generation.PolicyVersion && discovery.Status == "Complete"
            && discovery.Arms.Count == 1 && discovery.Arms[0].StopReason == "CandidateBudgetReached"
            && discovery.Arms[0].Evaluations.Count == 46 && discovery.Arms[0].Proposals.Count <= 256,
            "Complete discovery must precede screening membership.");
        var arm = discovery.Arms[0];
        var parties = arm.Proposals.Where(p => p.Result == "evaluated").Select(p => p.Party!).ToArray();
        Measurements(TowerBossImprovement.Inputs(d), parties, arm.Evaluations, DiscoveryTrials);
        Require(HarnessJson.Hash(discovery.DiscoveryShortlist)
            == HarnessJson.Hash(TowerSuppliedCompositionSearch.IncumbentShortlist(d, discovery.Arms)),
            "Changed discovery checkpoint shortlist.");
        foreach (var party in parties) TowerBossDiscovery.ValidateParty(d, party);
        var ranked = TowerBossGeneration.Rank(arm.Evaluations).ToArray();
        var ids = d.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        ids.UnionWith(ranked.Where(r => !ids.Contains(r.Id)).Take(Candidates - 3).Select(r => r.Id).ToArray());
        Require(ids.Count == Candidates, "Missing screening challenger; no shortened screen.");
        return new(Version, HarnessJson.Hash(d), HarnessJson.Hash(discovery), DiscoveryFights, seeds.ToArray(),
            ranked.Where(r => ids.Contains(r.Id)).Select(r => parties.Single(p => p.Id == r.Id)).ToArray());
    }

    internal static PracticalScreeningResult Nominate(TowerBossDiscoveryDefinition d, BossGenerationResult discovery,
        IReadOnlyList<int> seeds, PracticalScreeningFreeze freeze, IReadOnlyList<BossDiscoveryMeasurement> rows)
    {
        Require(HarnessJson.Hash(freeze) == HarnessJson.Hash(Freeze(d, discovery, seeds)), "Changed screening membership or binding.");
        var input = TowerBossImprovement.Inputs(d) with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, _ => seeds) };
        Measurements(input, freeze.Candidates, rows, ScreeningTrials);
        Require(rows.Select(r => r.Id).SequenceEqual(freeze.Candidates.Select(p => p.Id)), "Reordered screening measurements.");
        var ranked = TowerBossGeneration.Rank(rows).ToArray();
        var ids = d.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        ids.UnionWith(ranked.Where(r => !ids.Contains(r.Id)).Take(2).Select(r => r.Id).ToArray());
        Require(ids.Count == 5, "Missing screened nominee; no discovery fallback.");
        // This order replaces discovery order only in the new pipeline's final selection.
        var nominees = ranked.Where(r => ids.Contains(r.Id)).Select(r => freeze.Candidates.Single(p => p.Id == r.Id)).ToArray();
        return new(Version, freeze, rows.ToArray(), DiscoveryFights + ScreeningFights, nominees);
    }
}
