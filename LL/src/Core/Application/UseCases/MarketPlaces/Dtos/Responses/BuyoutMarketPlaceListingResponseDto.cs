using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class BuyoutMarketPlaceListingResponseDto
{
    public required Guid ListingId { get; init; }
    public MarketPlaceListingDto? RemainingListing { get; init; }
    public required InventoryItemDto PurchasedItem { get; init; }
    public required int PurchasedQuantity { get; init; }
    public required long TotalPrice { get; init; }
    public required long BuyerCinders { get; init; }
    public required MarketplaceChangeSetDto Marketplace { get; init; }
}
