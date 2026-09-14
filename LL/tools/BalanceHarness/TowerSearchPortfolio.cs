using System.Globalization;

namespace BalanceHarness;

/// <summary>Equal-cost comparison of a single deep search and a fixed portfolio, merged before screening.</summary>
public static class TowerSearchPortfolio
{
    public const string Version = "independent-search-portfolio-v19";
    public const string Policy = "tower-search-portfolio-v1";
    public const string DeepComponent = "portfolio-deep-loadout-composition-joint";
    public const string Portfolio = "portfolio-loadout-composition-joint";
    public static readonly string[] Methods = [TowerSearchAllocation.Deep, DeepComponent, TowerSearchAllocation.IsolatedA, TowerSearchAllocation.IsolatedB];
    public static readonly string[] ComparisonMethods = [TowerSearchAllocation.Deep, Portfolio];
    public const int Candidates = 1536, Attempts = 16384, DiscoveryFights = 73728, MaximumFights = 159744, MaximumSeconds = 21600;
    public const long MaximumBytes = 8589934592;

    internal static int ConstructionBudget(string method) => method switch {
        TowerSearchAllocation.Deep => Candidates, DeepComponent => 768,
        TowerSearchAllocation.IsolatedA or TowerSearchAllocation.IsolatedB => 384,
        _ => throw new InvalidDataException("Unknown portfolio component.")
    };
    internal static int AttemptBudget(BossDiscoveryGeneration g, string method) => method switch {
        TowerSearchAllocation.Deep => g.MaximumAttemptsPerArm, DeepComponent => g.MaximumAttemptsPerArm / 2,
        TowerSearchAllocation.IsolatedA or TowerSearchAllocation.IsolatedB => g.MaximumAttemptsPerArm / 4,
        _ => throw new InvalidDataException("Unknown portfolio component.")
    };
    internal static string StreamId(string method, int seed) => method == DeepComponent
        ? Policy + "-deep-" + seed.ToString(CultureInfo.InvariantCulture) : TowerSearchAllocation.StreamId(method, seed);
}
