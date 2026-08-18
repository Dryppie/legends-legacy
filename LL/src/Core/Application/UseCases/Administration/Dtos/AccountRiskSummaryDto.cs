using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AccountRiskSummaryDto(
    Guid AccountId,
    Guid CharacterId,
    string AccountLabel,
    string CharacterName,
    int CharacterLevel,
    DateTime AccountCreatedUtc,
    DateTimeOffset? LastSessionUtc,
    int Score,
    AccountRiskSeverity Severity,
    AccountRiskSignalType? PrimarySignalType,
    string PrimaryReason,
    int ConnectedAccountCount,
    long IncomingCinders,
    long OutgoingCinders,
    int TransferCount,
    DateTimeOffset? FirstFlaggedAt,
    DateTimeOffset? LastTriggeredAt,
    DateTimeOffset EvaluatedAt,
    int EvaluationVersion,
    DateTimeOffset AnalysisWindowStart,
    bool EvidenceComplete,
    int AnalyzedTransferCount,
    AccountInvestigationStatus InvestigationStatus);

public sealed record AccountRiskPageDto(
    IReadOnlyList<AccountRiskSummaryDto> Entries,
    int Total,
    IReadOnlyDictionary<AccountRiskSeverity, int> Counts,
    DateTimeOffset? LastEvaluatedAt,
    DateTimeOffset? FirstEvidenceAt,
    long DirectTransferCount,
    long DirectItemTransferCount,
    int EvaluatedAccountCount,
    int EligibleAccountCount,
    int UpToDateAccountCount,
    int PendingEvaluationCount,
    int IncompleteEvaluationCount,
    int EvaluationVersion,
    int LookbackDays,
    int Page,
    int PageSize);
