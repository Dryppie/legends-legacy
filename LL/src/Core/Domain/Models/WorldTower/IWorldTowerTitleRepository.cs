namespace Domain.Models.WorldTower;

public sealed record TowerTitleRecipient(Guid AccountId, Guid CharacterId, Guid AttemptId, Guid RallyId);

public interface IWorldTowerTitleRepository
{
    Task LockCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TowerTitleRecipient>> GetMissingRecipientsAsync(
        string serverId, int floorNumber, Guid titleId, int batchSize, CancellationToken cancellationToken);
}
