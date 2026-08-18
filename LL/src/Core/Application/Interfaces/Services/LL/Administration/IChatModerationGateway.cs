namespace Application.Interfaces.Services.LL.Administration;

public sealed record ChatMuteGatewayRequest(
    Guid OperationId,
    Guid CharacterId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset? ExpiresAt);

public sealed record ChatUnmuteGatewayRequest(
    Guid OperationId,
    Guid RestrictionId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason);

public sealed record ChatModerationGatewayResult(
    bool IsSuccess,
    Guid? RestrictionId,
    bool WasAlreadyProcessed,
    string ErrorMessage);

public sealed record ChatRestrictionGatewaySnapshot(
    Guid Id,
    Guid CharacterId,
    string Reason,
    string CreatedBySubject,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? RevokedBySubject,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);

public sealed record ChatModerationHistoryGatewayEntry(
    Guid OperationId,
    string ActionType,
    Guid CharacterId,
    Guid RestrictionId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset OccurredAt);

public sealed record ChatModerationStateGatewayResult(
    bool IsSuccess,
    ChatRestrictionGatewaySnapshot? ActiveMute,
    IReadOnlyList<ChatModerationHistoryGatewayEntry> History,
    string ErrorMessage);

public sealed record ChatModerationAuditGatewayQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? ActionType,
    string? Actor,
    string? Reference,
    Guid? OperationId,
    IReadOnlyCollection<Guid> CharacterIds,
    Guid? RestrictionId,
    DateTimeOffset? BeforeOccurredAt,
    Guid? BeforeOperationId,
    int Limit);

public sealed record ChatModerationAuditGatewayResult(
    bool IsSuccess,
    IReadOnlyList<ChatModerationHistoryGatewayEntry> Entries,
    string ErrorMessage);

public interface IChatModerationGateway
{
    Task<ChatModerationStateGatewayResult> GetStateAsync(
        Guid characterId,
        int historyLimit,
        CancellationToken cancellationToken);

    Task<ChatModerationAuditGatewayResult> GetAuditAsync(
        ChatModerationAuditGatewayQuery query,
        CancellationToken cancellationToken);

    Task<ChatModerationGatewayResult> MuteAsync(
        ChatMuteGatewayRequest request,
        CancellationToken cancellationToken);

    Task<ChatModerationGatewayResult> UnmuteAsync(
        ChatUnmuteGatewayRequest request,
        CancellationToken cancellationToken);
}
