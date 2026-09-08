using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments.Progression;
using Application.Interfaces.Services.LL.Inventories;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Inventories;
using Common.Randomness;
using Services.LL.Interfaces;

namespace Services.LL.Inventories;

public sealed class SelectionCrateService : ISelectionCrateService
{
    private readonly IInventoryService _inventory;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IStarterEquipmentService? _starterEquipment;
    private readonly CombatAcquisitionCatalog? _equipmentCatalog;

    public SelectionCrateService(
        IInventoryService inventory,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory,
        IStarterEquipmentService? starterEquipment = null,
        CombatAcquisitionCatalog? equipmentCatalog = null)
    {
        _inventory = inventory;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
        _starterEquipment = starterEquipment;
        _equipmentCatalog = equipmentCatalog;
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

        if (definition.RandomEquipment is not null)
        {
            if (optionId != RandomEquipmentBoxCatalog.OpenOptionId)
                return Fail("Open this box to receive its random equipment.");
            return await OpenRandomEquipmentBoxAsync(characterId, container, definition, cancellationToken);
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

    private async Task<SelectionCrateOpenResult> OpenRandomEquipmentBoxAsync(
        Guid characterId,
        InventoryItem container,
        SelectionContainerDefinition definition,
        CancellationToken cancellationToken)
    {
        var catalog = _equipmentCatalog
            ?? throw new InvalidOperationException("Equipment catalog is required to open an equipment box.");
        var reward = definition.RandomEquipment!;
        var candidates = catalog.BaseDropDefinitions(reward.Rarity)
            .Where(candidate => reward.EquipmentTypes is null || reward.EquipmentTypes.Contains(
                catalog.Equipment.Evaluator.GetArchetype(candidate.ArchetypeId).EquipmentType))
            .ToArray();
        if (candidates.Length == 0)
            return Fail("The equipment in this box is currently unavailable.");

        // Use a fresh opening identity even when more boxes are added to an existing stack.
        var identity = new[] { definition.ItemBaseId, container.ItemInstanceId.ToString("N"),
            Guid.NewGuid().ToString("N") };
        var random = new Random(StableRandom.Seed(identity));
        var descriptors = Enumerable.Range(0, reward.Quantity).Select(index => EquipmentData.Create(
            EquipmentState.Award(
                StableRandom.Guid([.. identity, index.ToString(System.Globalization.CultureInfo.InvariantCulture)]),
                catalog.Equipment.Evaluator,
                candidates[random.Next(candidates.Length)].Id,
                reward.Tier,
                reward.Rank,
                new(EquipmentAwardKind.ProtectedReward, definition.ItemBaseId, string.Join(":", identity)),
                new(EquipmentOwnershipKind.UnboundPersonal, characterId)),
            catalog.Equipment.Evaluator)).ToArray();
        var bases = await _itemBases.GetItemBasesByIdsAsync(
            descriptors.Select(data => data.ItemBaseId).Distinct().ToArray(), cancellationToken);
        if (descriptors.Any(data => !bases.TryGetValue(data.ItemBaseId, out var itemBase)
            || itemBase is not EquipmentBase equipmentBase || itemBase.Stackable
            || equipmentBase.EquipmentType != data.EquipmentType))
            return Fail("The equipment in this box is currently unavailable.");

        if (!await _inventory.TryConsumeInventoryItemAsync(characterId, container.ItemInstanceId, cancellationToken))
            return Fail($"The {definition.DisplayName} could not be consumed.");

        var items = descriptors.Select(data =>
        {
            var instance = new EquipmentInstance
            {
                Id = data.State.Id,
                ItemBaseId = data.ItemBaseId,
                ItemBase = bases[data.ItemBaseId]
            };
            instance.ApplyProgressionData(data);
            return new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = instance.Id,
                ItemInstance = instance,
                Quantity = 1
            };
        }).ToList();
        await _inventory.AddItemsToInventory(
            characterId, items, ItemAcquisitionSources.SelectionContainer, cancellationToken);
        return new(true, null, items, definition.DisplayName);
    }

    private static SelectionCrateOpenResult Fail(string message) =>
        new(false, message, []);
}
