using Application.UseCases.MarketPlaces.Dtos.Responses;

namespace Application.WebSockets.Contracts;

public record MarketListingSoldMsg(
    Guid ListingId,
    Guid SellerId,
    int Quantity,
    long TotalPrice,
    long SellerCinders,
    MarketPlaceListingDto? RemainingListing) : GameEventMsg;
