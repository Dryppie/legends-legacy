namespace BalanceHarness;

public sealed partial class TowerBossPartyGenerator
{
    /// <summary>Substitute one authored coverage provider at every existing position; combat still decides its value.</summary>
    public BossGeneratedChoice ChangeCoverageProvider(Random random, PartyChoice parent)
        => ChangeCoverageProvider(random, parent, collective: false);

    /// <summary>Require a source provider carried by multiple characters; reject when no collective substitution is legal.</summary>
    public BossGeneratedChoice ChangeCollectiveCoverageProvider(Random random, PartyChoice parent)
        => ChangeCoverageProvider(random, parent, collective: true);

    private BossGeneratedChoice ChangeCoverageProvider(Random random, PartyChoice parent, bool collective)
    {
        if (Invalid(parent) is not null) throw new InvalidDataException("Provider substitution requires a legal generated parent.");
        var used = parent.Builds.Values.SelectMany(ids => ids).GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var options = new List<(string Kind, (string From, string To)[] Pairs)>();
        foreach (var group in CoverageGroups())
        {
            var ids = group.Select(f => f.EssenceId).Order(StringComparer.Ordinal).ToArray();
            var pairs = new List<(string From, string To)>();
            foreach (var from in ids.Where(id => used.ContainsKey(id) && (!collective || used[id] > 1)))
            {
                var carriers = parent.Builds.Values.Where(build => build.Contains(from)).ToArray();
                foreach (var to in ids.Where(to => to != from))
                {
                    if (input.OwnedCopies is not null && used.GetValueOrDefault(to) + carriers.Length > input.OwnedCopies.GetValueOrDefault(to)) continue;
                    if (carriers.Any(build => build.Any(id => id != from && StringComparer.OrdinalIgnoreCase.Equals(families[id], families[to])))) continue;
                    pairs.Add((from, to));
                }
            }
            if (pairs.Count > 0) options.Add((group.Key, pairs.ToArray()));
        }
        var operation = collective ? "collective-provider" : "coverage-provider";
        if (options.Count == 0) return new(null, operation, null,
            collective ? "no-compatible-collective-provider-substitution" : "no-compatible-provider-substitution");
        // Categories have equal opportunity; select legal substitutions without weighting them by past results or copy counts.
        var selected = options[random.Next(options.Count)]; var pair = selected.Pairs[random.Next(selected.Pairs.Length)];
        var builds = parent.Builds.ToDictionary(p => p.Key,
            p => (IReadOnlyList<string>)p.Value.Select(id => id == pair.From ? pair.To : id).ToArray());
        return Choice(builds, $"{operation}:{selected.Kind}:{pair.From}->{pair.To}:{used[pair.From]}", null);
    }
}
