using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem;

public record PurchaseChampionMarketItemCommand(Guid CharacterId, string ItemId, int Quantity)
    : ICommand<Response<PurchaseChampionMarketItemResponseDto>>;

public sealed class PurchaseChampionMarketItemCommandHandler
    : IRequestHandler<PurchaseChampionMarketItemCommand, Response<PurchaseChampionMarketItemResponseDto>>
{
    private readonly IColosseumService _colosseumService;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public PurchaseChampionMarketItemCommandHandler(
        IColosseumService colosseumService,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _colosseumService = colosseumService;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<PurchaseChampionMarketItemResponseDto>> Handle(PurchaseChampionMarketItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _colosseumService.PurchaseChampionMarketItemAsync(
            request.CharacterId,
            request.ItemId,
            request.Quantity,
            cancellationToken);

        if (result is null)
        {
            return Response<PurchaseChampionMarketItemResponseDto>.Fail("Champion's Market purchase failed.");
        }

        var response = _mapper.Map<PurchaseChampionMarketItemResponseDto>(result);
        if (response.InventoryItemsGranted.Count > 0)
        {
            response.InventoryGrantId = Guid.NewGuid();
            await _outbox.EnqueueAsync(
                GameEventTypes.InventoryItemsGranted,
                new InventoryItemsGrantedPayload(
                    response.InventoryGrantId.Value,
                    request.CharacterId,
                    response.InventoryItemsGranted,
                    "champion-market",
                    "Champion's Market"),
                request.CharacterId,
                null,
                cancellationToken);
        }

        return Response<PurchaseChampionMarketItemResponseDto>.Success(response);
    }
}
