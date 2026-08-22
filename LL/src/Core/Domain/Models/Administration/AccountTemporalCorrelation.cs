namespace Domain.Models.Administration;

public enum AccountTemporalCorrelationAssessment
{
    InsufficientData,
    NoMaterialCorrelation,
    Low,
    Moderate,
    High
}

public sealed record AccountTemporalCorrelationAccountFact(
    Guid AccountId,
    Guid CharacterId,
    string CharacterName);

/// <summary>
/// Display-safe token metadata. Persistence resolves token hashes to row IDs before
/// this fact reaches the evaluator, so authentication secrets never enter results.
/// </summary>
public sealed record AccountTemporalTokenFact(
    long Id,
    Guid AccountId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    long? ReplacementId);

public sealed record AccountTemporalTransferFact(
    Guid TransferId,
    Guid SenderAccountId,
    Guid RecipientAccountId,
    DateTimeOffset OccurredAt);

public sealed record AccountTemporalCorrelationDataset(
    Guid SubjectAccountId,
    IReadOnlyDictionary<Guid, AccountTemporalCorrelationAccountFact> Accounts,
    IReadOnlyList<Guid> RelatedAccountIds,
    IReadOnlyList<AccountTemporalTokenFact> Tokens,
    IReadOnlyList<AccountTemporalTransferFact> Transfers,
    DateTimeOffset WindowStart,
    DateTimeOffset EvaluatedAt,
    bool EvidenceComplete,
    int AnalyzedTokenCount,
    int AnalyzedTransferCount);

public sealed record AccountTemporalCorrelationMatch(
    DateTimeOffset SubjectChainStartedAt,
    DateTimeOffset RelatedChainStartedAt,
    decimal DeltaMinutes,
    string Sequence,
    IReadOnlyList<Guid> NearbyTransferIds);

public sealed record AccountTemporalCorrelation(
    Guid RelatedAccountId,
    Guid RelatedCharacterId,
    string RelatedCharacterName,
    AccountTemporalCorrelationAssessment Assessment,
    string Summary,
    int SubjectChainStartCount,
    int RelatedChainStartCount,
    int SubjectActiveDays,
    int RelatedActiveDays,
    int SharedActiveDays,
    decimal ActiveDaySimilarity,
    int NearStartMatchCount,
    int StrongNearStartMatchCount,
    int RepeatedMatchDays,
    decimal MatchLift,
    decimal HourOfWeekSimilarity,
    int TransferAdjacentMatchCount,
    DateTimeOffset? FirstObservedAt,
    DateTimeOffset? LastObservedAt,
    DateTimeOffset WindowStart,
    DateTimeOffset EvaluatedAt,
    bool EvidenceComplete,
    int AnalyzedTokenCount,
    int AnalyzedTransferCount,
    int AnalysisVersion,
    IReadOnlyList<AccountTemporalCorrelationMatch> Matches,
    IReadOnlyList<string> Limitations);

public sealed record AccountTemporalCorrelationReport(
    Guid AccountId,
    DateTimeOffset WindowStart,
    DateTimeOffset EvaluatedAt,
    bool EvidenceComplete,
    int AnalyzedTokenCount,
    int AnalyzedTransferCount,
    int AnalysisVersion,
    IReadOnlyList<AccountTemporalCorrelation> Entries);

public sealed record AccountTemporalCorrelationPolicy(
    int AnalysisVersion,
    int MinimumActiveDays,
    int NearStartWindowMinutes,
    int StrongNearStartWindowMinutes,
    int TransferAdjacentWindowMinutes,
    int MinimumRepeatedMatchDays,
    int ModerateMinimumMatches,
    decimal ModerateMinimumLift,
    int HighMinimumRepeatedMatchDays,
    int HighMinimumMatches,
    decimal HighMinimumLift,
    int HighMinimumTransferAdjacentMatches,
    int MaximumDisplayedMatches);

/// <summary>
/// Produces an explainable investigation lead from token-chain timing. The result is
/// deliberately separate from the account-risk score and is never an ownership or
/// enforcement decision.
/// </summary>
public sealed class AccountTemporalCorrelationEvaluator(AccountTemporalCorrelationPolicy policy)
{
    private static readonly IReadOnlyList<string> KnownLimitations =
    [
        "Token-chain starts are not guaranteed logins; registration and some authenticated account operations also issue tokens.",
        "No device, network, user-agent, or authentication-method evidence is recorded.",
        "Shared households, scheduled events, and common regional play hours can produce similar timing."
    ];

    public IReadOnlyList<AccountTemporalCorrelation> Evaluate(AccountTemporalCorrelationDataset dataset)
    {
        if (!dataset.Accounts.ContainsKey(dataset.SubjectAccountId)) return [];

        var windowTokens = dataset.Tokens
            .Where(x => x.CreatedAt >= dataset.WindowStart && x.CreatedAt <= dataset.EvaluatedAt)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        var replacementIds = dataset.Tokens
            .Where(x => x.ReplacementId.HasValue)
            .Select(x => x.ReplacementId!.Value)
            .ToHashSet();
        var subjectTokens = windowTokens.Where(x => x.AccountId == dataset.SubjectAccountId).ToList();
        var subjectStarts = subjectTokens.Where(x => !replacementIds.Contains(x.Id)).ToList();

        return dataset.RelatedAccountIds
            .Distinct()
            .Where(dataset.Accounts.ContainsKey)
            .Select(relatedId => EvaluatePair(
                dataset,
                dataset.Accounts[relatedId],
                subjectTokens,
                subjectStarts,
                windowTokens.Where(x => x.AccountId == relatedId).ToList(),
                windowTokens.Where(x => x.AccountId == relatedId && !replacementIds.Contains(x.Id)).ToList()))
            .OrderByDescending(x => x.Assessment)
            .ThenByDescending(x => x.MatchLift)
            .ThenByDescending(x => x.NearStartMatchCount)
            .ThenBy(x => x.RelatedCharacterName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AccountTemporalCorrelation EvaluatePair(
        AccountTemporalCorrelationDataset dataset,
        AccountTemporalCorrelationAccountFact related,
        IReadOnlyList<AccountTemporalTokenFact> subjectTokens,
        IReadOnlyList<AccountTemporalTokenFact> subjectStarts,
        IReadOnlyList<AccountTemporalTokenFact> relatedTokens,
        IReadOnlyList<AccountTemporalTokenFact> relatedStarts)
    {
        var subjectDays = ActiveDays(subjectTokens);
        var relatedDays = ActiveDays(relatedTokens);
        var sharedDays = subjectDays.Intersect(relatedDays).Count();
        var unionDays = subjectDays.Union(relatedDays).Count();
        var activeDaySimilarity = unionDays == 0 ? 0m : (decimal)sharedDays / unionDays;
        var pairTransfers = dataset.Transfers
            .Where(x =>
                (x.SenderAccountId == dataset.SubjectAccountId && x.RecipientAccountId == related.AccountId) ||
                (x.SenderAccountId == related.AccountId && x.RecipientAccountId == dataset.SubjectAccountId))
            .OrderBy(x => x.OccurredAt)
            .ToList();
        var matches = MatchStarts(subjectStarts, relatedStarts, pairTransfers);
        var repeatedMatchDays = matches
            .Select(x => Later(x.SubjectChainStartedAt, x.RelatedChainStartedAt).UtcDateTime.Date)
            .Distinct()
            .Count();
        var strongMatches = matches.Count(x => Math.Abs(x.DeltaMinutes) <= policy.StrongNearStartWindowMinutes);
        var transferAdjacent = matches
            .SelectMany(x => x.NearbyTransferIds)
            .Distinct()
            .Count();
        var expectedMatches = ExpectedMatches(subjectStarts, relatedStarts);
        var matchLift = matches.Count == 0
            ? 0m
            : Math.Min(99m, matches.Count / Math.Max(0.25m, expectedMatches));
        var hourSimilarity = HourOfWeekSimilarity(subjectTokens, relatedTokens);
        var assessment = Assess(
            dataset.EvidenceComplete,
            subjectDays.Count,
            relatedDays.Count,
            matches.Count,
            repeatedMatchDays,
            matchLift,
            transferAdjacent);
        var observations = matches
            .SelectMany(x => new[] { x.SubjectChainStartedAt, x.RelatedChainStartedAt })
            .OrderBy(x => x)
            .ToList();

        return new AccountTemporalCorrelation(
            related.AccountId,
            related.CharacterId,
            related.CharacterName,
            assessment,
            Summary(assessment, matches.Count, repeatedMatchDays, matchLift, transferAdjacent),
            subjectStarts.Count,
            relatedStarts.Count,
            subjectDays.Count,
            relatedDays.Count,
            sharedDays,
            Round(activeDaySimilarity),
            matches.Count,
            strongMatches,
            repeatedMatchDays,
            Round(matchLift),
            Round(hourSimilarity),
            transferAdjacent,
            observations.Count == 0 ? null : observations[0],
            observations.Count == 0 ? null : observations[^1],
            dataset.WindowStart,
            dataset.EvaluatedAt,
            dataset.EvidenceComplete,
            subjectTokens.Count + relatedTokens.Count,
            pairTransfers.Count,
            policy.AnalysisVersion,
            matches.OrderByDescending(x => Later(x.SubjectChainStartedAt, x.RelatedChainStartedAt))
                .Take(Math.Max(1, policy.MaximumDisplayedMatches))
                .ToList(),
            KnownLimitations);
    }

    private List<AccountTemporalCorrelationMatch> MatchStarts(
        IReadOnlyList<AccountTemporalTokenFact> subjectStarts,
        IReadOnlyList<AccountTemporalTokenFact> relatedStarts,
        IReadOnlyList<AccountTemporalTransferFact> transfers)
    {
        var matches = new List<AccountTemporalCorrelationMatch>();
        var subjectIndex = 0;
        var relatedIndex = 0;
        var nearWindow = TimeSpan.FromMinutes(policy.NearStartWindowMinutes);
        var transferWindow = TimeSpan.FromMinutes(policy.TransferAdjacentWindowMinutes);

        while (subjectIndex < subjectStarts.Count && relatedIndex < relatedStarts.Count)
        {
            var subject = subjectStarts[subjectIndex];
            var related = relatedStarts[relatedIndex];
            var delta = related.CreatedAt - subject.CreatedAt;
            if (delta.Duration() <= nearWindow)
            {
                var nearbyTransfers = transfers
                    .Where(x =>
                        (x.OccurredAt - subject.CreatedAt).Duration() <= transferWindow ||
                        (x.OccurredAt - related.CreatedAt).Duration() <= transferWindow)
                    .Select(x => x.TransferId)
                    .Distinct()
                    .Take(20)
                    .ToList();
                matches.Add(new AccountTemporalCorrelationMatch(
                    subject.CreatedAt,
                    related.CreatedAt,
                    Round((decimal)delta.TotalMinutes),
                    delta >= TimeSpan.Zero ? "SubjectThenRelated" : "RelatedThenSubject",
                    nearbyTransfers));
                subjectIndex++;
                relatedIndex++;
            }
            else if (subject.CreatedAt < related.CreatedAt)
            {
                subjectIndex++;
            }
            else
            {
                relatedIndex++;
            }
        }

        return matches;
    }

    private decimal ExpectedMatches(
        IReadOnlyList<AccountTemporalTokenFact> subjectStarts,
        IReadOnlyList<AccountTemporalTokenFact> relatedStarts)
    {
        var subjectByDay = subjectStarts.GroupBy(x => x.CreatedAt.UtcDateTime.Date)
            .ToDictionary(x => x.Key, x => x.Count());
        var relatedByDay = relatedStarts.GroupBy(x => x.CreatedAt.UtcDateTime.Date)
            .ToDictionary(x => x.Key, x => x.Count());
        var probabilityWindow = Math.Min(1m, 2m * policy.NearStartWindowMinutes / (24m * 60m));
        return subjectByDay.Keys.Union(relatedByDay.Keys)
            .Sum(day => probabilityWindow *
                subjectByDay.GetValueOrDefault(day) *
                relatedByDay.GetValueOrDefault(day));
    }

    private AccountTemporalCorrelationAssessment Assess(
        bool complete,
        int subjectActiveDays,
        int relatedActiveDays,
        int matches,
        int repeatedMatchDays,
        decimal lift,
        int transferAdjacent)
    {
        if (!complete || subjectActiveDays < policy.MinimumActiveDays || relatedActiveDays < policy.MinimumActiveDays)
            return AccountTemporalCorrelationAssessment.InsufficientData;
        if (repeatedMatchDays >= policy.HighMinimumRepeatedMatchDays &&
            matches >= policy.HighMinimumMatches &&
            lift >= policy.HighMinimumLift &&
            transferAdjacent >= policy.HighMinimumTransferAdjacentMatches)
            return AccountTemporalCorrelationAssessment.High;
        if (repeatedMatchDays >= policy.MinimumRepeatedMatchDays &&
            matches >= policy.ModerateMinimumMatches &&
            lift >= policy.ModerateMinimumLift)
            return AccountTemporalCorrelationAssessment.Moderate;
        if (repeatedMatchDays >= 2 && matches >= 2)
            return AccountTemporalCorrelationAssessment.Low;
        return AccountTemporalCorrelationAssessment.NoMaterialCorrelation;
    }

    private static HashSet<DateTime> ActiveDays(IEnumerable<AccountTemporalTokenFact> tokens) =>
        tokens.Select(x => x.CreatedAt.UtcDateTime.Date).ToHashSet();

    private static decimal HourOfWeekSimilarity(
        IReadOnlyList<AccountTemporalTokenFact> subject,
        IReadOnlyList<AccountTemporalTokenFact> related)
    {
        if (subject.Count == 0 || related.Count == 0) return 0m;
        var a = new decimal[168];
        var b = new decimal[168];
        foreach (var token in subject) a[((int)token.CreatedAt.DayOfWeek * 24) + token.CreatedAt.Hour]++;
        foreach (var token in related) b[((int)token.CreatedAt.DayOfWeek * 24) + token.CreatedAt.Hour]++;
        var dot = a.Zip(b, (left, right) => left * right).Sum();
        var magnitudeA = (decimal)Math.Sqrt((double)a.Sum(x => x * x));
        var magnitudeB = (decimal)Math.Sqrt((double)b.Sum(x => x * x));
        return magnitudeA == 0 || magnitudeB == 0 ? 0m : dot / (magnitudeA * magnitudeB);
    }

    private static string Summary(
        AccountTemporalCorrelationAssessment assessment,
        int matches,
        int days,
        decimal lift,
        int transferAdjacent) => assessment switch
        {
            AccountTemporalCorrelationAssessment.InsufficientData =>
                "There is not enough complete token-chain history to assess temporal correlation.",
            AccountTemporalCorrelationAssessment.NoMaterialCorrelation =>
                "No repeated, material token-chain timing correlation was detected in the analysis window.",
            _ => $"{assessment} temporal correlation: {matches} near chain start(s) on {days} distinct day(s), " +
                 $"{Round(lift):0.##}x the activity-adjusted expectation; {transferAdjacent} retained transfer(s) were timing-adjacent."
        };

    private static DateTimeOffset Later(DateTimeOffset left, DateTimeOffset right) => left >= right ? left : right;
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
