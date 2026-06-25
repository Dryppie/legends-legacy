using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Services.LL.Professions.Craftings;

public sealed class CraftingItemCatalogService : ICraftingItemCatalogService
{
    private readonly IItemBaseRepository _itemBases;

    public CraftingItemCatalogService(IItemBaseRepository itemBases)
    {
        _itemBases = itemBases;
    }

    public async Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) =>
        await _itemBases.GetCraftableEquipmentBaseAsync(itemBaseId, cancellationToken);
}
