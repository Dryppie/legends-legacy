namespace Domain.Models.Administration;

public enum SupportCaseStatus { Open, Waiting, Resolved, Closed }
public enum SupportCaseEntryKind { Created, Note, StatusChanged, OperationLinked }

public sealed class SupportCase
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string? ExternalReference { get; set; }
    public SupportCaseStatus Status { get; set; }
    public string? Resolution { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
