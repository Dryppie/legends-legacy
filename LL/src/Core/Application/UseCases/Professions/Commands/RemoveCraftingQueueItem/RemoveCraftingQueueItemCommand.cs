using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.RemoveCraftingQueueItem;

public record RemoveCraftingQueueItemCommand(Guid CharacterId, string QueueItemId) : ICommand<Response<RemoveCraftingQueueItemResponseDto>>;

public class RemoveCraftingQueueItemCommandHandler : IRequestHandler<RemoveCraftingQueueItemCommand, Response<RemoveCraftingQueueItemResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public RemoveCraftingQueueItemCommandHandler(
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
        RemoveCraftingQueueItemCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueItemId, out var queueItemId))
            return Response<RemoveCraftingQueueItemResponseDto>.Fail("Failed to remove item in crafting queue");

        var removed = await _craftingService.RemoveCraftingQueueItemsAsync(
            request.CharacterId,
            [queueItemId],
            cancellationToken);

        if (!removed)
            return Response<RemoveCraftingQueueItemResponseDto>.Fail("Failed to remove item in crafting queue");

        var inventory = await _inventoryService.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);

        if (inventory == null)
            return Response<RemoveCraftingQueueItemResponseDto>.Fail("Failed to load updated inventory.");

        var action = await _characterActionService.GetCharacterActionAsync(
            request.CharacterId,
            cancellationToken);

        return Response<RemoveCraftingQueueItemResponseDto>.Success(new RemoveCraftingQueueItemResponseDto
        {
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems),
            CurrentAction = _mapper.Map<CharacterActionDto?>(action)
        });
    }
}
