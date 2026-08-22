using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AccountTemporalCorrelationMatchDto(
    DateTimeOffset SubjectChainStartedAt,
    DateTimeOffset RelatedChainStartedAt,
    decimal DeltaMinutes,
    string Sequence,
    IReadOnlyList<Guid> NearbyTransferIds);

public sealed record AccountTemporalCorrelationDto(
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
    IReadOnlyList<AccountTemporalCorrelationMatchDto> Matches,
    IReadOnlyList<string> Limitations);

public sealed record AccountTemporalCorrelationReportDto(
    Guid AccountId,
    DateTimeOffset WindowStart,
    DateTimeOffset EvaluatedAt,
    bool EvidenceComplete,
    int AnalyzedTokenCount,
    int AnalyzedTransferCount,
    int AnalysisVersion,
    IReadOnlyList<AccountTemporalCorrelationDto> Entries);
