using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.CharacterActions.CharacterActionDetails;
using MediatR;

namespace Application.UseCases.Professions.Commands.CancelTemperingQueue;

public record CancelTemperingQueueCommand(Guid CharacterId)
    : ICommand<Response<RemoveCraftingQueueItemResponseDto>>;

public sealed class CancelTemperingQueueCommandHandler
    : IRequestHandler<CancelTemperingQueueCommand, Response<RemoveCraftingQueueItemResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public CancelTemperingQueueCommandHandler(
        ICraftingService craftingService,
        IInventoryService inventoryService,
        ICharacterActionService characterActionService,
        IMapper mapper)
    {
        _craftingService = craftingService;
        _inventoryService = inventoryService;
        _characterActionService = characterActionService;
        _mapper = mapper;
    }

    public async Task<Response<RemoveCraftingQueueItemResponseDto>> Handle(
        CancelTemperingQueueCommand request,
        CancellationToken cancellationToken)
    {
        var action = await _characterActionService.PeekCharacterActionAsync(
            request.CharacterId,
            cancellationToken);
        var queueItems = action?.ActionDetails is CraftingActionDetails craftingDetails
            ? craftingDetails.CraftingQueueItems
            : action?.PausedTemperingQueueItems ?? [];
        var queueItemIds = queueItems
            .Select(item => item.Id)
            .Distinct()
            .ToList();

        if (queueItemIds.Count > 0)
        {
            var removed = await _craftingService.RemoveCraftingQueueItemsAsync(
                request.CharacterId,
                queueItemIds,
                cancellationToken);
            if (!removed)
            {
                return Response<RemoveCraftingQueueItemResponseDto>.Fail(
                    "Failed to cancel the Tempering queue.");
            }
        }

        var inventory = await _inventoryService.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (inventory == null)
        {
            return Response<RemoveCraftingQueueItemResponseDto>.Fail(
                "Failed to load updated inventory.");
        }

        if (action != null)
        {
            action.PausedTemperingQueueItems = [];
        }

        return Response<RemoveCraftingQueueItemResponseDto>.Success(
            new RemoveCraftingQueueItemResponseDto
            {
                InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems),
                CurrentAction = _mapper.Map<CharacterActionDto?>(action)
            });
    }
}
