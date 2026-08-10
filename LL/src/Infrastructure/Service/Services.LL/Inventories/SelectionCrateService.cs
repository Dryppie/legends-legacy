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

    public async Task<SelectionCrateOpenResult> OpenSelectionContainerAsync(
        Guid characterId,
        Guid containerItemInstanceId,
        string optionId,
        CancellationToken cancellationToken)
    {
        var container = await _inventory.GetInventoryItemAsync(
            characterId,
            containerItemInstanceId,
            cancellationToken);
        if (container is null || container.Quantity <= 0)
        {
            return Fail("The selection container was not found in your inventory.");
        }

        var definition = SelectionContainerCatalog.Find(container.ItemInstance.ItemBaseId);
        if (definition is null)
        {
            return Fail("This item is not a selection container.");
        }

        var option = definition.Options.FirstOrDefault(candidate =>
            candidate.Id.Equals(optionId, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            return Fail($"Select a valid {definition.SelectionLabel.ToLowerInvariant()} before opening the container.");
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            [option.ItemId],
            cancellationToken);
        if (!itemBases.TryGetValue(option.ItemId, out var rewardItemBase))
        {
            return Fail($"The selected {definition.SelectionLabel.ToLowerInvariant()} is currently unavailable.");
        }

        if (!await _inventory.TryConsumeInventoryItemAsync(
                characterId,
                containerItemInstanceId,
                cancellationToken))
        {
            return Fail($"The {definition.DisplayName} could not be consumed.");
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
