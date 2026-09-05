using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments.Progression;
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
    private readonly IStarterEquipmentService? _starterEquipment;
    private readonly EquipmentBlueprintCatalog? _blueprints;

    public SelectionCrateService(
        IInventoryService inventory,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory,
        IStarterEquipmentService? starterEquipment = null,
        EquipmentBlueprintCatalog? blueprints = null)
    {
        _inventory = inventory;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
        _starterEquipment = starterEquipment;
        _blueprints = blueprints;
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

        var definition = SelectionContainerCatalog.Find(container.ItemInstance.ItemBaseId, _blueprints);
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

        if (definition.ItemBaseId.Equals(TutorialArmsChestCatalog.ItemBaseId, StringComparison.OrdinalIgnoreCase))
        {
            var claim = await (_starterEquipment
                ?? throw new InvalidOperationException("Starter equipment service is required to open an Arms Chest.")).ClaimAsync(
                characterId,
                StarterEquipmentGrantKind.FirstWeapon,
                [option.Id],
                cancellationToken);
            if (claim.Grant is null || claim.Error is not null)
                return Fail(claim.Error ?? "The selected starter weapon could not be awarded.");
            if (claim.Rewards.Count == 0)
                return Fail("You have already opened your Arms Chest.");
            if (!await _inventory.TryConsumeInventoryItemAsync(characterId, containerItemInstanceId, cancellationToken))
                throw new InvalidOperationException("The Arms Chest could not be consumed after its weapon was awarded.");
            return new(true, null, claim.Rewards, definition.DisplayName, RewardsAlreadyPublished: true);
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
        await _inventory.AddItemsToInventory(
            characterId,
            rewards,
            ItemAcquisitionSources.SelectionContainer,
            cancellationToken);

        return new SelectionCrateOpenResult(true, null, rewards, definition.DisplayName);
    }

    private static SelectionCrateOpenResult Fail(string message) =>
        new(false, message, []);
}
