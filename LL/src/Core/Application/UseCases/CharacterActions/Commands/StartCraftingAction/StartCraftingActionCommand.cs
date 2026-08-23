using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Professions.Dtos;
using Common.Primitives;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCraftingAction;
public record StartCraftingActionCommand(Guid CharacterId, string QueueId, string ItemInstanceId)
    : ICommand<Response<TemperingQueueMutationResponseDto>>;
public class StartCraftingActionCommandHandler
    : IRequestHandler<StartCraftingActionCommand, Response<TemperingQueueMutationResponseDto>>
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

    public async Task<Response<TemperingQueueMutationResponseDto>> Handle(
        StartCraftingActionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueId, out var queueId) ||
            !Guid.TryParse(request.ItemInstanceId, out var itemInstanceId))
            return Response<TemperingQueueMutationResponseDto>.Fail("Unable to start crafting.");

        var inventoryItem = await _inventoryService.GetInventoryItemAsync(
            request.CharacterId,
            itemInstanceId,
            cancellationToken);
        if (inventoryItem?.ItemInstance is not EquipmentInstance equipmentInstance)
            return Response<TemperingQueueMutationResponseDto>.Fail("Unable to start crafting.");

        if (equipmentInstance.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return Response<TemperingQueueMutationResponseDto>.Fail("Tools cannot be modified through Crafting.");

        if (equipmentInstance.Rarity >= Rarity.Legacy)
            return Response<TemperingQueueMutationResponseDto>.Fail("Legacy items cannot be tempered.");

        var temperingProfile = _temperingProfileResolver.ResolveFor(equipmentInstance);
        if (temperingProfile == null)
            return Response<TemperingQueueMutationResponseDto>.Fail("No tempering profile applies to this item.");

        if ((equipmentInstance.Potential ?? 0) < TemperingConstants.PotentialCost)
            return Response<TemperingQueueMutationResponseDto>.Fail("Item does not have enough Potential.");

        var queueItem = new CraftingQueueItem
        {
            Id = queueId,
            EquipmentInstanceId = itemInstanceId,
            EquipmentInstance = equipmentInstance
        };

        var action = await _characterActionService.UpdateCraftingCharacterActionAsync(
            request.CharacterId,
            queueItem,
            inventoryItem,
            cancellationToken);
        return action is null
            ? Response<TemperingQueueMutationResponseDto>.Fail("Unable to start crafting.")
            : Response<TemperingQueueMutationResponseDto>.Success(
                new TemperingQueueMutationResponseDto
                {
                    RemovedInventoryItemIds = [itemInstanceId],
                    AddedQueueItemId = queueId,
                    Action = TemperingActionStateDto.From(action)
                });
    }
}
