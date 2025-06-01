using Application.Interfaces.Services.LL;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;
public record BuyoutMarketPlaceListingCommand(Guid CharacterId, BuyoutMarketPlaceListingRequest Buyout) : IRequest<Response<bool>>;
public class BuyoutMarketPlaceListingCommandHandler : IRequestHandler<BuyoutMarketPlaceListingCommand, Response<bool>>
{
    private readonly IMarketPlaceService _marketPlaceService;

    public BuyoutMarketPlaceListingCommandHandler(IMarketPlaceService marketPlaceService)
    {
        _marketPlaceService = marketPlaceService;
    }

    public async Task<Response<bool>> Handle(BuyoutMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (request.Buyout.Quantity <= 0) return Response<bool>.Fail("Failed to buy order.");

        return await _marketPlaceService.BuyoutMarketPlaceListingAsync(request.CharacterId, request.Buyout.MarketPlaceListingId, request.Buyout.Quantity, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to buy order.");
    }
}
