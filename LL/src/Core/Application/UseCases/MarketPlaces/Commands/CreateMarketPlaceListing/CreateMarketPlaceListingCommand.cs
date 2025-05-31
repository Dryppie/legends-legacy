using Application.Interfaces.Services.LL;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
public record CreateMarketPlaceListingCommand(Guid CharacterId, CreateMarketPlaceListingRequest Listing) : IRequest<Response<bool>>;
public class CreateMarketPlaceListingCommandHandler : IRequestHandler<CreateMarketPlaceListingCommand, Response<bool>>
{
    private readonly IMarketPlaceService _marketPlaceService;

    public CreateMarketPlaceListingCommandHandler(IMarketPlaceService marketPlaceService)
    {
        _marketPlaceService = marketPlaceService;
    }

    public async Task<Response<bool>> Handle(CreateMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        var marketPlaceListing = new MarketPlaceListing()
        {
            SellerId = request.CharacterId,
            ItemInstanceId = request.Listing.ItemInstanceId,
            Quantity = request.Listing.Quantity,
            UnitPrice = request.Listing.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _marketPlaceService.CreateMarketPlaceListingAsync(request.CharacterId, marketPlaceListing, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to create marketplace listing.");
    }
}
