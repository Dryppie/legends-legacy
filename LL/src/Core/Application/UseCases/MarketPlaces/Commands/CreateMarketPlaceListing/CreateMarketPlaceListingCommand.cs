using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
public record CreateMarketPlaceListingCommand(Guid CharacterId, CreateMarketPlaceListingRequest Listing) : ICommand<Response<MarketPlaceListingDto>>;
public class CreateMarketPlaceListingCommandHandler : IRequestHandler<CreateMarketPlaceListingCommand, Response<MarketPlaceListingDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public CreateMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task<Response<MarketPlaceListingDto>> Handle(CreateMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (request.Listing.Quantity <= 0 || request.Listing.UnitPrice <= 0) return Response<MarketPlaceListingDto>.Fail("Failed to create marketplace listing.");
        var marketPlaceListing = new MarketPlaceListing()
        {
            SellerId = request.CharacterId,
            ItemInstanceId = request.Listing.ItemInstanceId,
            Quantity = request.Listing.Quantity,
            UnitPrice = request.Listing.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var listing = await _marketPlaceService.CreateMarketPlaceListingAsync(request.CharacterId, marketPlaceListing, cancellationToken);
        if (listing == null) return Response<MarketPlaceListingDto>.Fail("Failed to create marketplace listing.");

        var dto = _mapper.Map<MarketPlaceListingDto>(listing);

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketListingCreatedMsg(dto));

        return Response<MarketPlaceListingDto>.Success(dto);
    }
}
