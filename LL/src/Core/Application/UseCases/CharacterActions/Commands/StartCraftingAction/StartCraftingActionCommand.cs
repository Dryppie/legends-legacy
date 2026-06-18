using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCraftingAction;
public record StartCraftingActionCommand(Guid CharacterId, string QueueId, string ItemInstanceId) : ICommand<Response<bool>>;
public class StartCraftingActionCommandHandler : IRequestHandler<StartCraftingActionCommand, Response<bool>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IInventoryService _inventoryService;

    public StartCraftingActionCommandHandler(
        ICharacterActionService characterActionService,
        IInventoryService inventoryService)
    {
        _characterActionService = characterActionService;
        _inventoryService = inventoryService;
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
