using Application.UseCases.Administration.Dtos;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Mappings;

public static class AccountRiskDtoMapper
{
    public static AccountRiskSummaryDto ToDto(AccountRiskSnapshotView view) =>
        ToDto(view.Snapshot, view.InvestigationStatus);

    public static AccountRiskSummaryDto ToDto(AccountRiskSnapshot snapshot, AccountInvestigationStatus status) => new(
        snapshot.AccountId,
        snapshot.CharacterId,
        snapshot.AccountLabel,
        snapshot.CharacterName,
        snapshot.CharacterLevel,
        snapshot.AccountCreatedUtc,
        snapshot.LastSessionUtc,
        snapshot.Score,
        snapshot.Severity,
        snapshot.PrimarySignalType,
        snapshot.PrimaryReason,
        snapshot.ConnectedAccountCount,
        snapshot.IncomingCinders,
        snapshot.OutgoingCinders,
        snapshot.TransferCount,
        snapshot.FirstFlaggedAt,
        snapshot.LastTriggeredAt,
        snapshot.EvaluatedAt,
        status);

    public static AccountRiskDetailsDto ToDto(AccountRiskDetails details) => new(
        ToDto(details.Snapshot, details.InvestigationStatus),
        details.Signals.Select(x => new AccountRiskSignalDto(x.Type, x.Category, x.Contribution, x.Title, x.Explanation, x.Evidence)).ToList(),
        details.Relationships.Select(x => new AccountRiskRelationshipDto(x.AccountId, x.CharacterId, x.CharacterName, x.Relationship, x.SentToSubject, x.ReceivedFromSubject, x.TransactionCount, x.YoungAccount, x.RiskScore, x.RiskSeverity, x.ItemTransfersToSubject, x.ItemTransfersFromSubject)).ToList(),
        details.Transfers.Select(x => new AccountRiskTransferDto(x.TransferId, x.Direction, x.Kind, x.CounterpartyAccountId, x.CounterpartyCharacterId, x.CounterpartyCharacterName, x.AssetId, x.AssetName, x.Quantity, x.OccurredAt)).ToList(),
        details.History.Select(x => new AccountRiskHistoryPointDto(x.Id, x.Score, x.Severity, x.EvaluatedAt)).ToList(),
        details.Notes.Select(ToDto).ToList());

    public static AccountRiskPageDto ToDto(AccountRiskPage page, int pageNumber, int pageSize) => new(
        page.Entries.Select(ToDto).ToList(),
        page.Total,
        page.Counts,
        page.LastEvaluatedAt,
        page.FirstEvidenceAt,
        page.DirectTransferCount,
        page.DirectItemTransferCount,
        page.EvaluatedAccountCount,
        pageNumber,
        pageSize);

    public static AccountRiskNoteDto ToDto(AccountRiskNote note) =>
        new(note.Id, note.ActorSubject, note.ActorDisplayName, note.Body, note.CreatedAt);
}
