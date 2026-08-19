namespace Domain.Models.Administration;

public enum AdminActionType
{
    AccountBanned,
    AccountBanRevoked,
    CompensationItemsGranted,
    AuditExported,
    AccountRiskStatusChanged,
    AccountRiskNoteAdded,
    MultiplayerRestricted,
    MultiplayerRestrictionRevoked
}

public enum AdministrationRiskLevel
{
    Normal,
    Permanent,
    HighValue
}

/// <summary>
/// Append-only record of a privileged operation. The identifier is supplied by the
/// operator client and doubles as the idempotency key.
/// </summary>
public sealed class AdminAction
{
    public Guid Id { get; set; }
    public AdminActionType ActionType { get; set; }
    public string Permission { get; set; } = string.Empty;
    public string ActorSubject { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public Guid? TargetAccountId { get; set; }
    public Guid? TargetCharacterId { get; set; }
    public Guid? TargetResourceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string DetailsJson { get; set; } = "{}";
    public AdministrationRiskLevel RiskLevel { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
