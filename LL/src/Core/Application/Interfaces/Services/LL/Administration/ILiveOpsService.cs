using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface ILiveOpsService
{
    Task<IReadOnlyList<PlayerAdministrationSnapshot>> SearchPlayersAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<AdministrationOperationResult<AccountBanOperation>> BanAccountAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    Task<AdministrationOperationResult<AccountBanOperation>> RevokeAccountBanAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken);

    Task<AdministrationOperationResult<ItemGrantOperation>> GrantCompensationItemsAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string itemBaseId,
        int quantity,
        string reason,
        string? internalNotes,
        CancellationToken cancellationToken);
}
