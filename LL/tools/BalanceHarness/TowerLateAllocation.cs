namespace BalanceHarness;

public sealed record TowerLateAllocationPrefix(string Method, int Evaluations, int Attempts,
    IReadOnlyList<BossDiscoveryMeasurement> Ranking);
public sealed record TowerLateAllocationDecision(int Seed, string SelectedMethod,
    IReadOnlyList<TowerLateAllocationPrefix> Prefixes);

/// <summary>One declared allocation after two independent 256-evaluation prefixes.</summary>
public static class TowerLateAllocation
{
    public const string Version = "independent-late-allocation-v18";
    public const string Policy = "tower-late-allocation-v1";
    public const int Prefix = 256, Continued = 512, Initial = 96;
    public const int MaximumFights = 114688, MaximumSeconds = 10800;

    internal static string Choose(BossDiscoveryMeasurement a, BossDiscoveryMeasurement b) =>
        TowerBossGeneration.Rank(new[] { a, b }).First() == a
            ? TowerSearchAllocation.IsolatedA : TowerSearchAllocation.IsolatedB;

    internal static TowerLateAllocationDecision Decide(int seed, BossGenerationArm a, BossGenerationArm b)
    {
        if (a.Method != TowerSearchAllocation.IsolatedA || b.Method != TowerSearchAllocation.IsolatedB
            || a.Seed != seed || b.Seed != seed || a.Evaluations.Count < Prefix || b.Evaluations.Count < Prefix)
            throw new InvalidDataException("Both complete declared allocation prefixes are required.");
        TowerLateAllocationPrefix Capture(BossGenerationArm arm)
        {
            var last = arm.Evaluations[Prefix - 1].Id;
            var attempts = arm.Proposals.TakeWhile(p => p.Result != "evaluated" || p.Party?.Id != last).Count() + 1;
            if (attempts > arm.Proposals.Count || arm.Proposals.Take(attempts).Count(p => p.Result == "evaluated") != Prefix)
                throw new InvalidDataException("Allocation prefix proposals do not match complete measurements.");
            return new(arm.Method, Prefix, attempts, TowerBossGeneration.Rank(arm.Evaluations.Take(Prefix)).ToArray());
        }
        var left = Capture(a); var right = Capture(b);
        return new(seed, Choose(left.Ranking[0], right.Ranking[0]), [left, right]);
    }

    internal static int FinalBudget(BossGenerationResult g, BossGenerationArm arm) =>
        !TowerSearchAllocation.IsComponent(arm.Method) ? g.Version == TowerSearchPortfolio.Version ? TowerSearchPortfolio.ConstructionBudget(arm.Method) : 768
            : g.AllocationDecisions!.Single(d => d.Seed == arm.Seed).SelectedMethod == arm.Method ? Continued : Prefix;

    internal static void ValidateComplete(BossDiscoveryGeneration policy, BossGenerationResult g)
    {
        if (policy.PolicyVersion is not (Version or TowerSearchPortfolio.Version))
        {
            if (g.AllocationDecisions is not null) throw new InvalidDataException("Historical policies cannot carry late allocation decisions.");
            return;
        }
        if (g.AllocationDecisions is null || !g.AllocationDecisions.Select(d => d.Seed).SequenceEqual(policy.Seeds))
            throw new InvalidDataException("One ordered allocation decision per root is required.");
        foreach (var seed in policy.Seeds)
        {
            var pair = TowerSearchAllocation.Components(g, TowerSearchAllocation.Isolated, seed);
            var expected = Decide(seed, pair[0], pair[1]);
            if (HarnessJson.Hash(expected) != HarnessJson.Hash(g.AllocationDecisions.Single(d => d.Seed == seed))
                || pair.Any(a => a.Evaluations.Count != FinalBudget(g, a)))
                throw new InvalidDataException("Allocation inputs, chosen component or final counts differ.");
        }
    }
}
