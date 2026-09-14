using System.Globalization;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerAllocationOrigin(string Method, int Seed, int Rank, string PartyId, string ProposalId);
public sealed record TowerAllocationRank(int Rank, string Id, IReadOnlyList<TowerAllocationOrigin> Sources);
public sealed record TowerAllocationUnit(string Method, int Seed, int Evaluations, IReadOnlyList<TowerAllocationRank> Ranking);
internal sealed record TowerAllocationCandidate(TowerRescreenCandidate Candidate, BossDiscoveryMeasurement Measurement,
    IReadOnlyList<TowerAllocationOrigin> Sources);

/// <summary>Compare one deep v13 search with two isolated v13 searches; merge only after complete generation.</summary>
public static class TowerSearchAllocation
{
    public const string Version = "independent-search-allocation-v17";
    public const string Policy = "tower-search-allocation-v1";
    public const string Deep = "loadout-composition-joint";
    public const string IsolatedA = "isolated-a-loadout-composition-joint";
    public const string IsolatedB = "isolated-b-loadout-composition-joint";
    public const string Isolated = "isolated-loadout-composition-joint";
    public static readonly string[] Methods = [Deep, IsolatedA, IsolatedB];
    public static readonly string[] ComparisonMethods = [Deep, Isolated];
    public const int DiscoveryFights = 36864;
    public const int MaximumFights = 106496;

    internal static bool IsComponent(string method) => method is IsolatedA or IsolatedB;
    internal static string StreamId(string method, int seed) => (method switch {
        Deep => "coverage-joint-", IsolatedA => Policy + "-isolated-a-", IsolatedB => Policy + "-isolated-b-",
        _ => throw new InvalidDataException("Unknown allocation component.")
    }) + seed.ToString(CultureInfo.InvariantCulture);

    internal static BossGenerationArm[] Components(BossGenerationResult generation, string method, int seed) => method switch {
        Deep => [generation.Arms.Single(a => a.Seed == seed && a.Method == Deep)],
        TowerSearchPortfolio.Portfolio when generation.Version == TowerSearchPortfolio.Version => [
            generation.Arms.Single(a => a.Seed == seed && a.Method == TowerSearchPortfolio.DeepComponent),
            generation.Arms.Single(a => a.Seed == seed && a.Method == IsolatedA),
            generation.Arms.Single(a => a.Seed == seed && a.Method == IsolatedB)],
        Isolated => [generation.Arms.Single(a => a.Seed == seed && a.Method == IsolatedA),
            generation.Arms.Single(a => a.Seed == seed && a.Method == IsolatedB)],
        _ => throw new InvalidDataException("Unknown allocation comparison unit.")
    };

    internal static TowerAllocationCandidate[] Candidates(TowerBossDiscoveryDefinition d, BossGenerationResult generation, string method, int seed)
    {
        var byRecipe = new Dictionary<string, TowerAllocationCandidate>(StringComparer.Ordinal);
        foreach (var arm in Components(generation, method, seed))
        {
            var proposals = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            foreach (var (row, rank) in TowerBossGeneration.Rank(arm.Evaluations).Select((e, i) => (e, i + 1)))
            {
                var p = proposals[row.Id]; var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
                var id = TowerFeedbackBenchmark.Id(scenario);
                var origin = new TowerAllocationOrigin(arm.Method, seed, rank, row.Id, p.Provenance.Id);
                if (!byRecipe.TryGetValue(id, out var entry))
                    byRecipe.Add(id, new(new(id, row.Id, p.Provenance.Id, 0, scenario), row, [origin]));
                else
                {
                    // Identical recipes on the same discovery schedule must have identical outcomes.
                    // Retain both charged evaluations and origins; do not double their screening weight.
                    static string Scoring(BossDiscoveryMeasurement e) => HarnessJson.Hash(new { e.Fitness, e.Behavior,
                        Cells = e.Cells.Select(c => new { c.Context, c.Floor, c.Clears, c.Draws, c.GuardianHealth, c.Survival }) });
                    if (HarnessJson.Hash(entry.Candidate.Scenario) != HarnessJson.Hash(scenario)
                        || entry.Measurement.Id != row.Id || Scoring(entry.Measurement) != Scoring(row))
                        throw new InvalidDataException("Duplicate component recipe has conflicting complete discovery evidence.");
                    byRecipe[id] = entry with { Sources = [..entry.Sources, origin] };
                }
            }
        }
        var byParty = byRecipe.Values.ToDictionary(c => c.Measurement.Id);
        return TowerBossGeneration.Rank(byParty.Values.Select(c => c.Measurement)).Select((row, i) => {
            var entry = byParty[row.Id]; return entry with { Candidate = entry.Candidate with { OriginalRank = i + 1 } };
        }).ToArray();
    }

    internal static TowerFeedbackShortlist Freeze(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery)
    {
        var arms = new List<TowerFeedbackArm>(); var originals = new List<TowerSearchArm>(); var units = new List<TowerAllocationUnit>();
        foreach (var seed in d.Generation.Seeds)
        foreach (var method in TowerGenerationComparisonDesign.FromDefinition(d).ComparisonMethods)
        {
            var rows = Candidates(d, discovery.Generation!, method, seed);
            if (rows.Length < TowerFeedbackBenchmark.Width) throw new InvalidDataException("Allocation unit cannot fill its distinct top 32.");
            arms.Add(new(method, seed, rows.Take(TowerFeedbackBenchmark.Width).Select(r => r.Candidate).ToArray()));
            originals.Add(new(method, seed, rows[0].Candidate.Id, rows[1].Candidate.Id));
            units.Add(new(method, seed, Components(discovery.Generation!, method, seed).Sum(a => a.Evaluations.Count),
                rows.Select(r => new TowerAllocationRank(r.Candidate.OriginalRank, r.Candidate.Id, r.Sources)).ToArray()));
        }
        return JsonSerializer.Deserialize<TowerFeedbackShortlist>(JsonSerializer.Serialize(new TowerFeedbackShortlist(
            1, TowerGenerationComparisonDesign.FromDefinition(d).Policy, HarnessJson.Hash(d), HarnessJson.Hash(discovery), originals, arms, units), HarnessJson.Options), HarnessJson.Options)!;
    }
}
