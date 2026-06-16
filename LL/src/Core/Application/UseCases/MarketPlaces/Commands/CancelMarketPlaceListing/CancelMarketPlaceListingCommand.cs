using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
public record CancelMarketPlaceListingCommand(Guid CharacterId, string ListingId) : ICommand<Response<CancelMarketPlaceListingResponseDto>>;
public class CancelMarketPlaceListingCommandHandler : IRequestHandler<CancelMarketPlaceListingCommand, Response<CancelMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public CancelMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task<Response<CancelMarketPlaceListingResponseDto>> Handle(CancelMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId)) return Response<CancelMarketPlaceListingResponseDto>.Fail("Invalid listing");

        var canceled = await _marketPlaceService.CancelMarketPlaceListingAsync(request.CharacterId, listingId, cancellationToken);
        if (canceled == null) return Response<CancelMarketPlaceListingResponseDto>.Fail("Failed to cancel listing");

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketListingCanceledMsg(listingId, request.CharacterId));

        return Response<CancelMarketPlaceListingResponseDto>.Success(new CancelMarketPlaceListingResponseDto
        {
            ListingId = listingId,
            ReturnedItem = _mapper.Map<Application.UseCases.Inventories.Dtos.InventoryItemDto>(canceled)
        });
    }
}
