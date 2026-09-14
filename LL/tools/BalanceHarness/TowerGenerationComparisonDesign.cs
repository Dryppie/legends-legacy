namespace BalanceHarness;

/// <summary>Explicitly versioned uses of the same frozen comparison and accounting contract.</summary>
internal sealed record TowerGenerationComparisonDesign(string Policy, string GenerationVersion, string[] Methods,
    int FeedbackSamples, int Controls, int FamilyCapacity)
{
    public bool IsAllocation => Policy is TowerSearchAllocation.Policy or TowerLateAllocation.Policy or TowerSearchPortfolio.Policy;
    public int MaximumAttempts => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.Attempts : 8192;
    public long MaximumBytes => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.MaximumBytes : TowerFeedbackBenchmarkRun.MaximumBytes;
    public int MaximumSeconds => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.MaximumSeconds : Policy == TowerLateAllocation.Policy ? TowerLateAllocation.MaximumSeconds : TowerFeedbackBenchmarkRun.MaximumSeconds;
    public const string LineagePolicy = "tower-party-lineages-v1";
    public const string RetentionPolicy = "tower-loadout-retention-v1";
    public int CandidatesPerArm => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.Candidates : IsAllocation ? 768 : 384;
    public int DiscoveryFights => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.DiscoveryFights : IsAllocation ? TowerSearchAllocation.DiscoveryFights : TowerFeedbackBenchmark.DiscoveryFights;
    public string[] ComparisonMethods => Policy == TowerSearchPortfolio.Policy ? TowerSearchPortfolio.ComparisonMethods : IsAllocation ? TowerSearchAllocation.ComparisonMethods : Methods;
    public int MaximumFights => DiscoveryFights + TowerFeedbackBenchmark.RescreenFights + FamilyCapacity * 512;
    public int Reservations => 3 + 8 + 64 + 512 + FeedbackSamples;
    public string CandidateMethod => ComparisonMethods[1];
    public static TowerGenerationComparisonDesign FromPolicy(string policy) => policy switch {
        TowerFeedbackBenchmark.Policy => new(policy, TowerGenerationFeedback.Version, TowerGenerationFeedback.Methods, 32, 36, 64),
        TowerSearchPortfolio.Policy => new(policy, TowerSearchPortfolio.Version, TowerSearchPortfolio.Methods, 0, 112, 144),
        TowerLateAllocation.Policy => new(policy, TowerLateAllocation.Version, TowerSearchAllocation.Methods, 0, 94, 128),
        TowerSearchAllocation.Policy => new(policy, TowerSearchAllocation.Version, TowerSearchAllocation.Methods, 0, 74, 112),
        LineagePolicy => new(policy, TowerPartyLineages.Version, TowerPartyLineages.Methods, 0, 62, 96),
        RetentionPolicy => new(policy, TowerLoadoutRetention.Version, TowerLoadoutRetention.Methods, 0, 48, 80),
        _ => throw new InvalidDataException("Unknown frozen generation comparison policy.")
    };
    public static TowerGenerationComparisonDesign FromDefinition(TowerBossDiscoveryDefinition d) => FromPolicy(d.Generation.PolicyVersion switch {
        TowerGenerationFeedback.Version => TowerFeedbackBenchmark.Policy,
        TowerLoadoutRetention.Version => RetentionPolicy,
        TowerPartyLineages.Version => LineagePolicy,
        TowerSearchPortfolio.Version => TowerSearchPortfolio.Policy,
        TowerLateAllocation.Version => TowerLateAllocation.Policy,
        TowerSearchAllocation.Version => TowerSearchAllocation.Policy,
        _ => throw new InvalidDataException("Unknown generation comparison version.")
    });
}
