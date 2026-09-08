using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Dungeons;

namespace Application.MediatR.Synchronization;

public sealed class DungeonInventoryStateSync(IDbContext db, IDungeonInventoryItemCatalog items)
{
    public async Task<IReadOnlySet<Guid>> GetAffectedCharacterIdsAsync(CancellationToken cancellationToken)
    {
        var changes = await db.GetInventoryQuantityChangesAsync(cancellationToken);
        return changes
            .Where(change => items.AffectsDungeonAvailability(change.ItemBaseId))
            .Select(change => change.CharacterId)
            .ToHashSet();
    }
}
