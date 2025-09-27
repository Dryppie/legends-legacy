
using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
public record CancelMarketPlaceListingCommand(Guid CharacterId, string ListingId) : ICommand<Response<bool>>;
public class CancelMarketPlaceListingCommandHandler : IRequestHandler<CancelMarketPlaceListingCommand, Response<bool>>
{
    private readonly IMarketPlaceService _marketPlaceService;

    public CancelMarketPlaceListingCommandHandler(IMarketPlaceService marketPlaceService)
    {
        _marketPlaceService = marketPlaceService;
    }

    public async Task<Response<bool>> Handle(CancelMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId)) return Response<bool>.Fail("Invalid listing");

        return await _marketPlaceService.CancelMarketPlaceListingAsync(request.CharacterId, listingId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to cancel listing");
    }
}
