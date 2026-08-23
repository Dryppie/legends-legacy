using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.CancelTemperingQueue;

public record CancelTemperingQueueCommand(Guid CharacterId)
    : ICommand<Response<TemperingQueueMutationResponseDto>>;

public sealed class CancelTemperingQueueCommandHandler
    : IRequestHandler<CancelTemperingQueueCommand, Response<TemperingQueueMutationResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IMapper _mapper;

    public CancelTemperingQueueCommandHandler(
        ICraftingService craftingService,
        IMapper mapper)
    {
        _craftingService = craftingService;
        _mapper = mapper;
    }

    public async Task<Response<TemperingQueueMutationResponseDto>> Handle(
        CancelTemperingQueueCommand request,
        CancellationToken cancellationToken)
    {
        var removed = await _craftingService.CancelTemperingQueueAsync(
            request.CharacterId,
            cancellationToken);

        return Response<TemperingQueueMutationResponseDto>.Success(
            new TemperingQueueMutationResponseDto
            {
                ReturnedInventoryItems = _mapper.Map<List<InventoryItemDto>>(removed.ReturnedInventoryItems),
                RemovedQueueItemIds = removed.RemovedQueueItemIds,
                Action = TemperingActionStateDto.From(removed.Action)
            });
    }
}
