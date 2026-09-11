using Application.Common.Interfaces;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.WorldTower;

public sealed class WorldTowerTitleRepository(IDbContext db) : IWorldTowerTitleRepository
{
    public Task LockCharacterAsync(Guid characterId, CancellationToken cancellationToken) =>
        db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);

    public async Task<IReadOnlyList<TowerTitleRecipient>> GetMissingRecipientsAsync(
        string serverId, int floorNumber, Guid titleId, int batchSize, CancellationToken cancellationToken)
    {
        return await db.TowerRallyParticipants.AsNoTracking()
            .Where(p => p.TowerRally.ServerId == serverId
                && p.TowerRally.FloorNumber == floorNumber
                && p.TowerRally.Attempt != null
                && p.TowerRally.Attempt.Status == TowerAttemptStatus.Succeeded
                && p.TowerRally.Attempt.Succeeded == true
                && p.TowerRally.Attempt.CompletedAt != null
                && db.Characters.Any(c => c.Id == p.CharacterId && c.UserId == p.AccountId)
                && !db.PlayerTitleUnlocks.Any(u => u.AccountId == p.AccountId
                    && u.CharacterId == p.CharacterId && u.TitleDefinitionId == titleId && u.SeasonId == null))
            .OrderBy(p => p.CharacterId).ThenBy(p => p.TowerRally.Attempt!.CompletedAt).ThenBy(p => p.Id)
            .Select(p => new TowerTitleRecipient(p.AccountId, p.CharacterId, p.TowerRally.Attempt!.Id, p.TowerRallyId))
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
