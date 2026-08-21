using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.BuyCommodity;

public sealed record BuyCommodityCommand(Guid CharacterId, BuyCommodityRequest Buy)
    : ICommand<Response<BuyCommodityResponseDto>>;

public sealed class BuyCommodityCommandHandler(
    IMarketPlaceService marketPlaceService,
    MarketplaceChangePublisher changePublisher,
    IMapper mapper)
    : IRequestHandler<BuyCommodityCommand, Response<BuyCommodityResponseDto>>
{
    public async Task<Response<BuyCommodityResponseDto>> Handle(
        BuyCommodityCommand request,
        CancellationToken cancellationToken)
    {
        var result = await marketPlaceService.BuyCommodityAsync(
            request.CharacterId,
            request.Buy.ItemBaseId,
            request.Buy.Quantity,
            request.Buy.MaximumUnitPrice,
            cancellationToken);
        if (result == null)
            return Response<BuyCommodityResponseDto>.Fail(
                "The requested quantity is no longer available within that price limit.");

        var marketplace = await changePublisher.PublishAsync(
            result.Fills.Select(fill => new MarketplaceListingChangeDto(
                fill.ListingId,
                mapper.Map<MarketPlaceListingDto?>(fill.RemainingListing))).ToArray(),
            [],
            result.Fills.Select(fill => mapper.Map<MarketPlaceOrderDto>(fill.Order)).ToArray(),
            result.Fills.Select(fill => fill.SellerId).Append(request.CharacterId),
            nameof(BuyCommodityCommand),
            cancellationToken);

        return Response<BuyCommodityResponseDto>.Success(new(
            result.FilledQuantity,
            result.TotalPrice,
            result.BuyerCinders,
            marketplace));
    }
}
