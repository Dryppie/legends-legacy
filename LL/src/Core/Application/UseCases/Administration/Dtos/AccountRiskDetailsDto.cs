using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AccountRiskSignalDto(
    AccountRiskSignalType Type,
    string Category,
    int Contribution,
    string Title,
    string Explanation,
    IReadOnlyDictionary<string, decimal> Evidence);

public sealed record AccountRiskRelationshipDto(
    Guid AccountId,
    Guid CharacterId,
    string CharacterName,
    string Relationship,
    long SentToSubject,
    long ReceivedFromSubject,
    int TransactionCount,
    bool YoungAccount,
    int? RiskScore,
    AccountRiskSeverity? RiskSeverity,
    int ItemTransfersToSubject,
    int ItemTransfersFromSubject);

public sealed record AccountRiskTransferDto(
    Guid TransferId,
    string Direction,
    string Kind,
    Guid CounterpartyAccountId,
    Guid CounterpartyCharacterId,
    string CounterpartyCharacterName,
    string AssetId,
    string AssetName,
    long Quantity,
    DateTimeOffset OccurredAt);

public sealed record AccountRiskHistoryPointDto(
    Guid Id,
    int Score,
    AccountRiskSeverity Severity,
    DateTimeOffset EvaluatedAt);

public sealed record AccountRiskNoteDto(
    Guid Id,
    string ActorSubject,
    string ActorDisplayName,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record AccountRiskDetailsDto(
    AccountRiskSummaryDto Account,
    IReadOnlyList<AccountRiskSignalDto> Signals,
    IReadOnlyList<AccountRiskRelationshipDto> Relationships,
    IReadOnlyList<AccountRiskTransferDto> Transfers,
    IReadOnlyList<AccountRiskHistoryPointDto> History,
    IReadOnlyList<AccountRiskNoteDto> Notes);
