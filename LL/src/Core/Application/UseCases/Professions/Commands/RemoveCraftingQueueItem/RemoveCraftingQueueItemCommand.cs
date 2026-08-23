using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.RemoveCraftingQueueItem;

public record RemoveCraftingQueueItemCommand(Guid CharacterId, string QueueItemId)
    : ICommand<Response<TemperingQueueMutationResponseDto>>;

public class RemoveCraftingQueueItemCommandHandler
    : IRequestHandler<RemoveCraftingQueueItemCommand, Response<TemperingQueueMutationResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IMapper _mapper;

    public RemoveCraftingQueueItemCommandHandler(
        ICraftingService craftingService,
        IMapper mapper)
    {
        _craftingService = craftingService;
        _mapper = mapper;
    }

    public async Task<Response<TemperingQueueMutationResponseDto>> Handle(
        RemoveCraftingQueueItemCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueItemId, out var queueItemId))
            return Response<TemperingQueueMutationResponseDto>.Fail("Failed to remove item in crafting queue");

        var removed = await _craftingService.RemoveCraftingQueueItemsAsync(
            request.CharacterId,
            [queueItemId],
            cancellationToken);

        if (removed is null)
            return Response<TemperingQueueMutationResponseDto>.Fail("Failed to remove item in crafting queue");

        return Response<TemperingQueueMutationResponseDto>.Success(new TemperingQueueMutationResponseDto
        {
            ReturnedInventoryItems = _mapper.Map<List<InventoryItemDto>>(removed.ReturnedInventoryItems),
            RemovedQueueItemIds = removed.RemovedQueueItemIds,
            Action = TemperingActionStateDto.From(removed.Action)
        });
    }
}
