using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
public record CancelMarketPlaceListingCommand(Guid CharacterId, string ListingId) : ICommand<Response<CancelMarketPlaceListingResponseDto>>;
public class CancelMarketPlaceListingCommandHandler : IRequestHandler<CancelMarketPlaceListingCommand, Response<CancelMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public CancelMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
        _mapper = mapper;
    }

    public async Task<Response<CancelMarketPlaceListingResponseDto>> Handle(CancelMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ListingId, out var listingId)) return Response<CancelMarketPlaceListingResponseDto>.Fail("Invalid listing");

        var canceled = await _marketPlaceService.CancelMarketPlaceListingAsync(request.CharacterId, listingId, cancellationToken);
        if (canceled == null) return Response<CancelMarketPlaceListingResponseDto>.Fail("Failed to cancel listing");

        var marketplace = await _changePublisher.PublishAsync(
            [new MarketplaceListingChangeDto(listingId, null)],
            [],
            [],
            [request.CharacterId],
            nameof(CancelMarketPlaceListingCommand),
            cancellationToken);

        return Response<CancelMarketPlaceListingResponseDto>.Success(new CancelMarketPlaceListingResponseDto
        {
            ListingId = listingId,
            ReturnedItem = _mapper.Map<Application.UseCases.Inventories.Dtos.InventoryItemDto>(canceled),
            Marketplace = marketplace
        });
    }
}
