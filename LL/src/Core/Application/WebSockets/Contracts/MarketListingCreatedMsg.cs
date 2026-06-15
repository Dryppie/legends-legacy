using Application.UseCases.MarketPlaces.Dtos.Responses;

namespace Application.WebSockets.Contracts;

public record MarketListingCreatedMsg(MarketPlaceListingDto Listing) : GameEventMsg;
