namespace BalanceHarness;

public sealed record LoadoutFitness(int EntryWins, int Wins, int Trials, double GuardianHealth, double Survival);
public sealed record LoadoutEvaluation(string Id, string Origin, string? Parent, IReadOnlyList<string> Essences,
    LoadoutFitness Fitness, IReadOnlyList<string> Trials);
public sealed record LoadoutProposal(int Attempt, string Origin, string? Parent, IReadOnlyList<string> Essences, string Result);
public sealed record LoadoutSearchResult(string Method, int Seed, string StopReason,
    IReadOnlyList<LoadoutEvaluation> Evaluations, IReadOnlyList<LoadoutProposal> Proposals);

/// <summary>Bounded deterministic search of ordered loadouts. Combat, not mechanic tags, supplies fitness.</summary>
public static class LoadoutSearch
{
    public const string Version = "tower-character-search-v1";

    public static IOrderedEnumerable<LoadoutEvaluation> Rank(IEnumerable<LoadoutEvaluation> candidates) => candidates
        .OrderByDescending(c => c.Fitness.EntryWins).ThenByDescending(c => c.Fitness.Wins)
        .ThenBy(c => c.Fitness.GuardianHealth).ThenByDescending(c => c.Fitness.Survival)
        .ThenBy(c => c.Id, StringComparer.Ordinal);

    public static IReadOnlyList<LoadoutEvaluation> Beam(IEnumerable<LoadoutEvaluation> candidates, int width)
    {
        var selected = new List<LoadoutEvaluation>();
        foreach (var item in Rank(candidates))
        {
            // Keep structurally different starts, even when their current discovery score is lower.
            if (selected.All(s => s.Essences.Zip(item.Essences).Count(p => p.First != p.Second) >= 2)) selected.Add(item);
            if (selected.Count == width) break;
        }
        return selected;
    }

    public static string[] Mutate(IReadOnlyList<string> parent, IReadOnlyList<string> pool, Random random, string kind)
    {
        var ids = parent.ToArray();
        var first = random.Next(ids.Length);
        var second = (first + 1 + random.Next(ids.Length - 1)) % ids.Length;
        if (kind == "order") (ids[first], ids[second]) = (ids[second], ids[first]);
        else
        {
            ids[first] = pool[random.Next(pool.Count)];
            if (kind == "double") ids[second] = pool[random.Next(pool.Count)];
            else if (kind != "single") throw new ArgumentException("Unknown mutation.");
        }
        return ids;
    }

    public static async Task<LoadoutSearchResult> RunAsync(string method, int seed, int candidates, int attempts,
        IReadOnlyList<string> control, IReadOnlyDictionary<string, string> families,
        IReadOnlyList<IReadOnlyList<string>> authored,
        Func<IReadOnlyList<string>, CancellationToken, Task<(LoadoutFitness Fitness, IReadOnlyList<string> Trials)>> evaluate,
        CancellationToken token = default, Action<LoadoutProposal>? onProposal = null, bool sharedStarts = false)
    {
        if (method is not ("guided" or "random") || candidates is < 2 or > 100 || attempts < candidates || attempts > 10000
            || control.Count is < 2 or > 10 || families.Count < control.Count
            || control.Any(id => !families.ContainsKey(id)) || control.Select(id => families[id]).Distinct().Count() != control.Count)
            throw new InvalidDataException("Invalid bounded loadout search.");
        var pool = families.Keys.Order(StringComparer.Ordinal).ToArray();
        var random = new Random(seed);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var evaluations = new List<LoadoutEvaluation>();
        var proposals = new List<LoadoutProposal>();
        string[] Sample()
        {
            // Partial Fisher-Yates: uniform ordered distinct-definition tuples, followed by family rejection.
            var choices = pool.ToArray();
            for (var i = 0; i < control.Count; i++)
            {
                var index = random.Next(i, choices.Length);
                (choices[i], choices[index]) = (choices[index], choices[i]);
            }
            return choices.Take(control.Count).ToArray();
        }
        for (var attempt = 0; attempt < attempts && evaluations.Count < candidates; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string origin; string? parent = null; string[] ids;
            if (attempt == 0) { origin = "control"; ids = control.ToArray(); }
            else if ((method == "guided" || sharedStarts) && attempt <= authored.Count)
            { origin = sharedStarts ? "shared-start" : "mechanics-start"; ids = authored[attempt - 1].ToArray(); }
            else if (method == "random" || evaluations.Count < 4 || attempt % 4 == 0)
            { origin = "random-restart"; ids = Sample(); }
            else
            {
                var beam = Beam(evaluations, 3);
                var source = beam[random.Next(beam.Count)]; parent = source.Id;
                origin = (attempt % 4) switch { 1 => "single", 2 => "double", _ => "order" };
                ids = Mutate(source.Essences, pool, random, origin);
            }
            var rejection = ids.Length != control.Count || ids.Any(id => !families.ContainsKey(id))
                ? "invalid-pool-or-count" : ids.Select(id => families[id]).Distinct().Count() != ids.Length ? "duplicate-family" : null;
            var id = HarnessJson.Hash(ids);
            if (rejection is null && !seen.Add(id)) rejection = "duplicate";
            var proposal = new LoadoutProposal(attempt, origin, parent, ids, rejection ?? "evaluated");
            proposals.Add(proposal); onProposal?.Invoke(proposal);
            if (rejection is not null) continue;
            var result = await evaluate(ids, token);
            evaluations.Add(new(id, origin, parent, ids, result.Fitness, result.Trials));
        }
        return new(method, seed, evaluations.Count == candidates ? "candidate-budget" : "proposal-budget", evaluations, proposals);
    }
}
