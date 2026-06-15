using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
public record CancelMarketPlaceListingCommand(Guid CharacterId, string ListingId) : ICommand<Response<bool>>;
public class CancelMarketPlaceListingCommandHandler : IRequestHandler<CancelMarketPlaceListingCommand, Response<bool>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;

    public CancelMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Response<bool>> Handle(CancelMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId)) return Response<bool>.Fail("Invalid listing");

        var canceled = await _marketPlaceService.CancelMarketPlaceListingAsync(request.CharacterId, listingId, cancellationToken);
        if (!canceled) return Response<bool>.Fail("Failed to cancel listing");

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketListingCanceledMsg(listingId, request.CharacterId));

        return Response<bool>.Success(true);
    }
}
