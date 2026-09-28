namespace Domain.Models.Administration;

/// <summary>Append-only case history; Id is the client operation's idempotency key.</summary>
public sealed class SupportCaseEntry
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public int Sequence { get; set; }
    public SupportCaseEntryKind Kind { get; set; }
    public string ActorSubject { get; set; } = "";
    public string ActorDisplayName { get; set; } = "";
    public string Body { get; set; } = "";
    public string? EvidenceReference { get; set; }
    public Guid? LinkedOperationId { get; set; }
    public string? LinkedSource { get; set; }
    public string RequestHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
