namespace Domain.Models.Administration;

public sealed record SupportCasePage(IReadOnlyList<SupportCase> Cases, int Total, int Page, int PageSize);
public sealed record SupportCaseDetails(SupportCase Case, IReadOnlyList<SupportCaseEntry> Entries, int? NextBeforeSequence);
public sealed record SupportCaseChange(Guid OperationId, Guid CaseId, Guid CharacterId, int ExpectedVersion,
    SupportCaseEntryKind Kind, string Body, string? Category = null, string? Title = null,
    string? ExternalReference = null, SupportCaseStatus? Status = null, string? EvidenceReference = null,
    Guid? LinkedOperationId = null, string? LinkedSource = null);

public interface ISupportCaseRepository
{
    Task LockAsync(Guid operationId, Guid caseId, CancellationToken ct);
    Task<SupportCase?> GetAsync(Guid id, CancellationToken ct);
    Task<SupportCaseEntry?> GetOperationAsync(Guid id, CancellationToken ct);
    Task<SupportCasePage> SearchAsync(Guid? characterId, SupportCaseStatus? status, string? search, int page, CancellationToken ct);
    Task<IReadOnlyList<SupportCaseEntry>> EntriesAsync(Guid caseId, int? beforeSequence, CancellationToken ct);
    void Add(SupportCase value);
    void Append(SupportCaseEntry value);
}
