using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.BuyCommodity;

public sealed record BuyCommodityCommand(Guid CharacterId, BuyCommodityRequest Buy)
    : ICommand<Response<BuyCommodityResponseDto>>;

public sealed class BuyCommodityCommandHandler(
    IMarketPlaceService marketPlaceService,
    IGameEventPublisher eventPublisher,
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

        foreach (var fill in result.Fills)
        {
            await eventPublisher.PublishAsync(
                new Audience.World(),
                new MarketListingSoldMsg(
                    fill.ListingId,
                    fill.SellerId,
                    fill.Quantity,
                    fill.TotalPrice,
                    fill.SellerCinders,
                    mapper.Map<MarketPlaceListingDto?>(fill.RemainingListing)));
        }

        return Response<BuyCommodityResponseDto>.Success(new(
            result.FilledQuantity,
            result.TotalPrice,
            result.BuyerCinders));
    }
}
