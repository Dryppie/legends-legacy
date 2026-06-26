using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCraftingAction;
public record StartCraftingActionCommand(Guid CharacterId, string QueueId, string ItemInstanceId) : ICommand<Response<bool>>;
public class StartCraftingActionCommandHandler : IRequestHandler<StartCraftingActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IInventoryService _inventoryService;
    private readonly ITemperingProfileResolver _temperingProfileResolver;

    public StartCraftingActionCommandHandler(
        ICharacterActionService characterActionService,
        IInventoryService inventoryService,
        ITemperingProfileResolver temperingProfileResolver)
    {
        _characterActionService = characterActionService;
        _inventoryService = inventoryService;
        _temperingProfileResolver = temperingProfileResolver;
    }

    public async Task<Response<bool>> Handle(StartCraftingActionCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueId, out var queueId) ||
            !Guid.TryParse(request.ItemInstanceId, out var itemInstanceId))
            return Response<bool>.Fail("Unable to start crafting.");

        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        var inventoryItem = inventory?.InventoryItems.FirstOrDefault(item => item.ItemInstanceId == itemInstanceId);
        if (inventoryItem?.ItemInstance is not EquipmentInstance equipmentInstance)
            return Response<bool>.Fail("Unable to start crafting.");

        if (equipmentInstance.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return Response<bool>.Fail("Tools cannot be modified through Crafting.");

        if (equipmentInstance.Rarity >= Rarity.Legacy)
            return Response<bool>.Fail("Legacy items cannot be tempered.");

        var temperingProfile = _temperingProfileResolver.ResolveFor(equipmentInstance);
        if (temperingProfile == null)
            return Response<bool>.Fail("No tempering profile applies to this item.");

        if ((equipmentInstance.Potential ?? 0) < TemperingConstants.PotentialCost)
            return Response<bool>.Fail("Item does not have enough Potential.");

        var queueItem = new CraftingQueueItem
        {
            Id = queueId,
            EquipmentInstanceId = itemInstanceId
        };

        var success = await _characterActionService.UpdateCraftingCharacterActionAsync(request.CharacterId, queueItem, cancellationToken);
        return success
            ? Response<bool>.Success(success)
            : Response<bool>.Fail("Unable to start crafting.");
    }
}
