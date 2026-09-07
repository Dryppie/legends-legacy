using Application.Common.Interfaces;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Inventories;

internal static class SigilFragmentRewards
{
    public static async Task<IReadOnlyList<InventoryItem>> GrantAsync(IDbContext context, Guid characterId, int amount, string source, CancellationToken ct)
    {
        if (amount <= 0) return [];
        var definition = await context.ItemBases.SingleAsync(x => x.Id == SigilFragmentItem.ItemBaseId, ct);
        var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = definition.Id, ItemBase = definition };
        var item = new InventoryItem { InventoryId = characterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = amount };
        await new InventoryRepository(context).AddItemsToInventory(characterId, [item], source, ct);
        return [item];
    }
}
