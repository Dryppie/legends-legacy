using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetAccountTemporalCorrelations;

public sealed record GetAccountTemporalCorrelationsQuery(Guid AccountId, int? WindowDays)
    : IQuery<Response<AccountTemporalCorrelationReportDto>>;

public sealed class GetAccountTemporalCorrelationsQueryHandler(IAccountTemporalCorrelationService service)
    : IRequestHandler<GetAccountTemporalCorrelationsQuery, Response<AccountTemporalCorrelationReportDto>>
{
    public async Task<Response<AccountTemporalCorrelationReportDto>> Handle(
        GetAccountTemporalCorrelationsQuery request,
        CancellationToken cancellationToken)
    {
        var report = await service.AnalyzeAsync(request.AccountId, request.WindowDays, cancellationToken);
        return report is null
            ? Response<AccountTemporalCorrelationReportDto>.Fail("The account-risk snapshot was not found.")
            : Response<AccountTemporalCorrelationReportDto>.Success(ToDto(report));
    }

    private static AccountTemporalCorrelationReportDto ToDto(AccountTemporalCorrelationReport report) => new(
        report.AccountId,
        report.WindowStart,
        report.EvaluatedAt,
        report.EvidenceComplete,
        report.AnalyzedTokenCount,
        report.AnalyzedTransferCount,
        report.AnalysisVersion,
        report.Entries.Select(entry => new AccountTemporalCorrelationDto(
            entry.RelatedAccountId,
            entry.RelatedCharacterId,
            entry.RelatedCharacterName,
            entry.Assessment,
            entry.Summary,
            entry.SubjectChainStartCount,
            entry.RelatedChainStartCount,
            entry.SubjectActiveDays,
            entry.RelatedActiveDays,
            entry.SharedActiveDays,
            entry.ActiveDaySimilarity,
            entry.NearStartMatchCount,
            entry.StrongNearStartMatchCount,
            entry.RepeatedMatchDays,
            entry.MatchLift,
            entry.HourOfWeekSimilarity,
            entry.TransferAdjacentMatchCount,
            entry.FirstObservedAt,
            entry.LastObservedAt,
            entry.WindowStart,
            entry.EvaluatedAt,
            entry.EvidenceComplete,
            entry.AnalyzedTokenCount,
            entry.AnalyzedTransferCount,
            entry.AnalysisVersion,
            entry.Matches.Select(match => new AccountTemporalCorrelationMatchDto(
                match.SubjectChainStartedAt,
                match.RelatedChainStartedAt,
                match.DeltaMinutes,
                match.Sequence,
                match.NearbyTransferIds)).ToList(),
            entry.Limitations)).ToList());
}
