namespace Domain.Models.Administration;

public sealed record AccountRiskSearch(
    string? Search,
    AccountRiskSeverity? MinimumSeverity,
    AccountRiskSignalType? SignalType,
    AccountInvestigationStatus? Status,
    int? MinimumScore,
    int? MaximumAccountAgeDays,
    DateTimeOffset? LastTriggeredAfter,
    string Sort,
    int Page,
    int PageSize);

public sealed record AccountRiskPage(
    IReadOnlyList<AccountRiskSnapshotView> Entries,
    int Total,
    IReadOnlyDictionary<AccountRiskSeverity, int> Counts,
    DateTimeOffset? LastEvaluatedAt);

public sealed record AccountRiskSnapshotView(
    AccountRiskSnapshot Snapshot,
    AccountInvestigationStatus InvestigationStatus);

public sealed record AccountRiskTransferEvidence(
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

public sealed record AccountRiskDetails(
    AccountRiskSnapshot Snapshot,
    AccountInvestigationStatus InvestigationStatus,
    IReadOnlyList<AccountRiskSignal> Signals,
    IReadOnlyList<AccountRiskRelationship> Relationships,
    IReadOnlyList<AccountRiskTransferEvidence> Transfers,
    IReadOnlyList<AccountRiskHistory> History,
    IReadOnlyList<AccountRiskNote> Notes);

public interface IAccountRiskRepository
{
    Task AcquireEvaluationLockAsync(CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetLastEvaluatedAtAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetCandidateAccountIdsAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken);
    Task<AccountRiskAnalysisDataset> GetAnalysisDatasetAsync(IReadOnlyCollection<Guid> accountIds, DateTimeOffset since, int maximumTransfers, CancellationToken cancellationToken);
    Task UpsertEvaluationsAsync(IReadOnlyCollection<AccountRiskEvaluation> evaluations, DateTimeOffset evaluatedAt, int historyMinimumScoreChange, CancellationToken cancellationToken);
    Task<AccountRiskPage> SearchAsync(AccountRiskSearch search, CancellationToken cancellationToken);
    Task<AccountRiskDetails?> GetDetailsAsync(Guid accountId, int transferLimit, CancellationToken cancellationToken);
    Task<AccountRiskInvestigation?> GetInvestigationAsync(Guid accountId, CancellationToken cancellationToken);
    Task<AccountRiskSnapshot?> GetSnapshotAsync(Guid accountId, CancellationToken cancellationToken);
    void AddInvestigation(AccountRiskInvestigation investigation);
    void AddNote(AccountRiskNote note);
    void AddAdminAction(AdminAction action);
}
