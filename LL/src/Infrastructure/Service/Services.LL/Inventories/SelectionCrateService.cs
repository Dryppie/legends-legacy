using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Inventories;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Items;
using Services.LL.Interfaces;

namespace Services.LL.Inventories;

public sealed class SelectionCrateService : ISelectionCrateService
{
    private readonly IInventoryService _inventory;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryItemFactory _inventoryItemFactory;

    public SelectionCrateService(
        IInventoryService inventory,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory)
    {
        _inventory = inventory;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
    }

    public async Task<SelectionCrateOpenResult> OpenCatalystSelectionCrateAsync(
        Guid characterId,
        Guid crateItemInstanceId,
        string optionId,
        CancellationToken cancellationToken)
    {
        var crate = await _inventory.GetInventoryItemAsync(
            characterId,
            crateItemInstanceId,
            cancellationToken);
        if (crate is null ||
            crate.Quantity <= 0 ||
            !crate.ItemInstance.ItemBaseId.Equals(
                CatalystSelectionCrateCatalog.ItemBaseId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Fail("The Catalyst Selection Crate was not found in your inventory.");
        }

        var option = CatalystSelectionCrateCatalog.Options.FirstOrDefault(candidate =>
            candidate.Id.Equals(optionId, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            return Fail("Select a valid catalyst before opening the crate.");
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            [option.ItemId],
            cancellationToken);
        if (!itemBases.TryGetValue(option.ItemId, out var rewardItemBase))
        {
            return Fail("The selected catalyst is currently unavailable.");
        }

        if (!await _inventory.TryConsumeInventoryItemAsync(
                characterId,
                crateItemInstanceId,
                cancellationToken))
        {
            return Fail("The Catalyst Selection Crate could not be consumed.");
        }

        var rewards = _inventoryItemFactory
            .CreateForQuantity(rewardItemBase, option.Quantity, characterId)
            .ToList();
        await _inventory.AddItemsToInventory(characterId, rewards, cancellationToken);

        return new SelectionCrateOpenResult(true, null, rewards);
    }

    private static SelectionCrateOpenResult Fail(string message) =>
        new(false, message, []);
}
