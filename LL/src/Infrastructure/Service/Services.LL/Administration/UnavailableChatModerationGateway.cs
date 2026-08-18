using Application.Interfaces.Services.LL.Administration;

namespace Services.LL.Administration;

/// <summary>
/// Fail-closed registration for hosts that share the Application assembly but are
/// not allowed to call Chat moderation. API.LiveOps replaces this registration
/// with its authenticated HTTP gateway.
/// </summary>
public sealed class UnavailableChatModerationGateway : IChatModerationGateway
{
    private const string Error =
        "Chat moderation is only available through the LiveOps API host.";

    public Task<ChatModerationStateGatewayResult> GetStateAsync(
        Guid characterId,
        int historyLimit,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ChatModerationStateGatewayResult(
            false,
            null,
            [],
            Error));

    public Task<ChatModerationAuditGatewayResult> GetAuditAsync(
        ChatModerationAuditGatewayQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ChatModerationAuditGatewayResult(
            false,
            [],
            Error));

    public Task<ChatModerationGatewayResult> MuteAsync(
        ChatMuteGatewayRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ChatModerationGatewayResult(
            false,
            null,
            false,
            Error));

    public Task<ChatModerationGatewayResult> UnmuteAsync(
        ChatUnmuteGatewayRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ChatModerationGatewayResult(
            false,
            null,
            false,
            Error));
}
