using Domain.Models.Dungeons;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Application.Common.Interfaces;
using Persistence.LL.Repositories.Inventories;

namespace Persistence.LL.Repositories.Dungeons;

public sealed class DungeonSigilAssemblyRepository(IDbContext context) : IDungeonSigilAssemblyRepository
{
    public async Task<long?> TrySpendFragmentsAsync(Guid characterId, long amount, CancellationToken cancellationToken)
    {
        if (amount <= 0 || amount > int.MaxValue) return null;
        var inventory = new InventoryRepository(context);
        var owned = await inventory.GetInventoryQuantityAsync(characterId, SigilFragmentItem.ItemBaseId, cancellationToken);
        if (!await inventory.TryRemoveItemsByBaseIdAsync(characterId,
                new() { [SigilFragmentItem.ItemBaseId] = (int)amount }, cancellationToken)) return null;
        // The command transaction stays open. Flush spending before its database-backed
        // inventory and dungeon-hub response is rebuilt, including removal of empty stacks.
        await context.SaveChangesAsync(cancellationToken);
        return owned - amount;
    }
}
