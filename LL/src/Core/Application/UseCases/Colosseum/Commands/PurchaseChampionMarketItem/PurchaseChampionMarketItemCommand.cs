using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
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
    private readonly IMapper _mapper;

    public PurchaseChampionMarketItemCommandHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
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

        return Response<PurchaseChampionMarketItemResponseDto>.Success(_mapper.Map<PurchaseChampionMarketItemResponseDto>(result));
    }
}
