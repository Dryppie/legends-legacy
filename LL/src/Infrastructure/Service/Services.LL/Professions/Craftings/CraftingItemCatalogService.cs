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

    public async Task<IReadOnlyDictionary<string, EquipmentBase>> GetCraftableEquipmentBasesAsync(
        IReadOnlyCollection<string> itemBaseIds,
        CancellationToken cancellationToken)
    {
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemBaseIds, cancellationToken);
        return itemBases
            .Where(x => x.Value is EquipmentBase { EquipmentType: not EquipmentType.Tool })
            .ToDictionary(
                x => x.Key,
                x => (EquipmentBase)x.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
