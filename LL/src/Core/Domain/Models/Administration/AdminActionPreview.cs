namespace Domain.Models.Administration;

public static class AdminActionPreviewKinds
{
    public const string AccountBan = "AccountBan";
    public const string AccountBanRevoke = "AccountBanRevoke";
    public const string ChatMute = "ChatMute";
    public const string ChatUnmute = "ChatUnmute";
    public const string CompensationGrant = "CompensationGrant";
}

/// <summary>
/// Short-lived authorization to submit one exact privileged operation against the
/// state observed during preview. Request content is represented by a SHA-256 hash.
/// </summary>
public sealed class AdminActionPreview
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public string ActionKind { get; set; } = string.Empty;
    public string ActorSubject { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string StateHash { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
}
