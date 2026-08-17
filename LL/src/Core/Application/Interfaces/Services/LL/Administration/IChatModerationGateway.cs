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

public interface IChatModerationGateway
{
    Task<ChatModerationGatewayResult> MuteAsync(
        ChatMuteGatewayRequest request,
        CancellationToken cancellationToken);

    Task<ChatModerationGatewayResult> UnmuteAsync(
        ChatUnmuteGatewayRequest request,
        CancellationToken cancellationToken);
}
