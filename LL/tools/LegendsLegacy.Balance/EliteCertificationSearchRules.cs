namespace LegendsLegacy.Balance;

internal static class EliteCertificationSearchRules
{
    internal static IReadOnlyList<int> SelectPercentileCohortIndexes(
        IReadOnlyList<double> scores,
        double targetScore,
        int maximum)
    {
        ArgumentNullException.ThrowIfNull(scores);
        if (scores.Count == 0)
            throw new InvalidOperationException("Elite percentile cohort selection requires at least one candidate.");
        if (!double.IsFinite(targetScore))
            throw new ArgumentOutOfRangeException(nameof(targetScore));
        if (maximum < 1)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        return scores.Select((score, index) => new { Score = score, Index = index })
            .OrderBy(candidate => Math.Abs(candidate.Score - targetScore))
            .ThenByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Index)
            .Take(maximum)
            .Select(candidate => candidate.Index)
            .ToArray();
    }

    internal static bool IsScenarioImprovement(
        IReadOnlyDictionary<string, double> parentScores,
        IReadOnlyDictionary<string, double> challengerScores,
        string scenarioId,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(parentScores);
        ArgumentNullException.ThrowIfNull(challengerScores);
        if (!parentScores.ContainsKey(scenarioId) || !challengerScores.ContainsKey(scenarioId))
            throw new InvalidOperationException($"Scenario '{scenarioId}' is missing from a local-challenge score vector.");
        if (!double.IsFinite(tolerance) || tolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        if (parentScores.Keys.Any(key => !challengerScores.ContainsKey(key)))
            throw new InvalidOperationException("Local-challenge score vectors do not contain the same scenarios.");

        return challengerScores[scenarioId] - parentScores[scenarioId] > tolerance
               && parentScores.Keys.Where(key => !key.Equals(scenarioId, StringComparison.Ordinal))
                   .All(key => challengerScores[key] >= parentScores[key] - tolerance);
    }

    internal static long CountPartyGenomes(int candidateCount, int requiredSlots)
    {
        if (candidateCount < 1)
            throw new ArgumentOutOfRangeException(nameof(candidateCount));
        if (requiredSlots < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredSlots));

        var result = 1L;
        for (var index = 1; index <= requiredSlots; index++)
        {
            result = checked(result * (candidateCount + index - 1) / index);
        }
        return result;
    }
}
