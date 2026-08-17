namespace Domain.Models.Administration;

public interface IAdministrationRepository
{
    Task<AdminAction?> GetActionAsync(Guid operationId, CancellationToken cancellationToken);
    Task<AccountRestriction?> GetRestrictionAsync(Guid restrictionId, CancellationToken cancellationToken);
    Task<AccountRestriction?> GetActiveAccountBanAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<PlayerAdministrationSnapshot?> GetPlayerByAccountIdAsync(
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<PlayerAdministrationSnapshot?> GetPlayerByCharacterIdAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerAdministrationSnapshot>> SearchPlayersAsync(
        string query,
        int limit,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AdministrationItemCatalogEntry>> SearchItemsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AdministrationHistoryEntry>> GetHistoryAsync(
        Guid accountId,
        Guid characterId,
        int limit,
        CancellationToken cancellationToken);
    void AddAction(AdminAction action);
    void AddRestriction(AccountRestriction restriction);
}
