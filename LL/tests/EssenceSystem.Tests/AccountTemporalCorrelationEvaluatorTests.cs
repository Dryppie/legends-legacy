using Domain.Models.Administration;

namespace EssenceSystem.Tests;

public sealed class AccountTemporalCorrelationEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SubjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RelatedId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Repeated_chain_starts_with_independent_transfer_context_are_high_correlation()
    {
        var tokens = new List<AccountTemporalTokenFact>();
        var transfers = new List<AccountTemporalTransferFact>();
        long tokenId = 1;
        for (var day = 1; day <= 7; day++)
        {
            var subjectStart = Now.AddDays(-day).AddHours(-2);
            var relatedStart = subjectStart.AddMinutes(4);
            tokens.AddRange(Chain(SubjectId, subjectStart, ref tokenId));
            tokens.AddRange(Chain(RelatedId, relatedStart, ref tokenId));
            if (day <= 2)
            {
                transfers.Add(new AccountTemporalTransferFact(
                    Guid.NewGuid(), SubjectId, RelatedId, relatedStart.AddMinutes(2)));
            }
        }

        var result = Evaluator().Evaluate(Dataset(tokens, transfers)).Single();

        Assert.Equal(AccountTemporalCorrelationAssessment.High, result.Assessment);
        Assert.Equal(7, result.NearStartMatchCount);
        Assert.Equal(7, result.StrongNearStartMatchCount);
        Assert.Equal(7, result.RepeatedMatchDays);
        Assert.Equal(2, result.TransferAdjacentMatchCount);
        Assert.Equal(7, result.SubjectChainStartCount);
        Assert.Equal(7, result.RelatedChainStartCount);
        Assert.DoesNotContain(result.Limitations, x => x.Contains("hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Refresh_rotations_are_activity_pulses_but_not_chain_starts()
    {
        var tokens = new List<AccountTemporalTokenFact>();
        long tokenId = 1;
        for (var day = 1; day <= 7; day++)
        {
            tokens.AddRange(Chain(SubjectId, Now.AddDays(-day).AddHours(-2), ref tokenId));
            tokens.AddRange(Chain(RelatedId, Now.AddDays(-day).AddHours(5), ref tokenId));
        }

        var result = Evaluator().Evaluate(Dataset(tokens, [])).Single();

        Assert.Equal(7, result.SubjectChainStartCount);
        Assert.Equal(7, result.RelatedChainStartCount);
        Assert.Equal(0, result.NearStartMatchCount);
        Assert.Equal(AccountTemporalCorrelationAssessment.NoMaterialCorrelation, result.Assessment);
    }

    [Fact]
    public void Sparse_or_truncated_history_is_insufficient_even_when_timestamps_match()
    {
        var tokens = new List<AccountTemporalTokenFact>();
        long tokenId = 1;
        for (var day = 1; day <= 2; day++)
        {
            var start = Now.AddDays(-day);
            tokens.AddRange(Chain(SubjectId, start, ref tokenId));
            tokens.AddRange(Chain(RelatedId, start.AddMinutes(1), ref tokenId));
        }

        var sparse = Evaluator().Evaluate(Dataset(tokens, [])).Single();
        var incomplete = Evaluator().Evaluate(Dataset(tokens, [], complete: false)).Single();

        Assert.Equal(AccountTemporalCorrelationAssessment.InsufficientData, sparse.Assessment);
        Assert.Equal(AccountTemporalCorrelationAssessment.InsufficientData, incomplete.Assessment);
    }

    private static AccountTemporalCorrelationEvaluator Evaluator() => new(new AccountTemporalCorrelationPolicy(
        AnalysisVersion: 1,
        MinimumActiveDays: 7,
        NearStartWindowMinutes: 15,
        StrongNearStartWindowMinutes: 5,
        TransferAdjacentWindowMinutes: 15,
        MinimumRepeatedMatchDays: 3,
        ModerateMinimumMatches: 4,
        ModerateMinimumLift: 2,
        HighMinimumRepeatedMatchDays: 5,
        HighMinimumMatches: 6,
        HighMinimumLift: 3,
        HighMinimumTransferAdjacentMatches: 2,
        MaximumDisplayedMatches: 20));

    private static AccountTemporalCorrelationDataset Dataset(
        IReadOnlyList<AccountTemporalTokenFact> tokens,
        IReadOnlyList<AccountTemporalTransferFact> transfers,
        bool complete = true) => new(
        SubjectId,
        new Dictionary<Guid, AccountTemporalCorrelationAccountFact>
        {
            [SubjectId] = new(SubjectId, Guid.NewGuid(), "Subject"),
            [RelatedId] = new(RelatedId, Guid.NewGuid(), "Related")
        },
        [RelatedId],
        tokens,
        transfers,
        Now.AddDays(-90),
        Now,
        complete,
        tokens.Count,
        transfers.Count);

    private static IReadOnlyList<AccountTemporalTokenFact> Chain(
        Guid accountId,
        DateTimeOffset startedAt,
        ref long nextId)
    {
        var rootId = nextId++;
        var refreshId = nextId++;
        return
        [
            new(rootId, accountId, startedAt, startedAt.AddDays(30), startedAt.AddMinutes(30), refreshId),
            new(refreshId, accountId, startedAt.AddMinutes(30), startedAt.AddDays(30), null, null)
        ];
    }
}
