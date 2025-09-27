using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.RemoveCraftingQueueItem;
public record RemoveCraftingQueueItemCommand(Guid CharacterId, string QueueItemId) : ICommand<Response<bool>>;
public class RemoveCraftingQueueItemCommandHandler : IRequestHandler<RemoveCraftingQueueItemCommand, Response<bool>>
{
    private readonly ICraftingService _craftingService;
    public RemoveCraftingQueueItemCommandHandler(ICraftingService craftingService)
    {
        _craftingService = craftingService;
    }
    public async Task<Response<bool>> Handle(RemoveCraftingQueueItemCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.QueueItemId, out var queueItemId)) return Response<bool>.Fail("Failed to remove item in crafting queue");

        return await _craftingService.RemoveCraftingQueueItemsAsync(request.CharacterId, [queueItemId], cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to remove item in crafting queue");
    }
}