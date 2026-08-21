using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CreateMarketPlaceListingResponseDto
{
    public MarketPlaceListingDto? Listing { get; init; }
    public required Guid ListedItemInstanceId { get; init; }
    public required int ListedQuantity { get; init; }
    public required int FilledQuantity { get; init; }
    public required long FilledTotalPrice { get; init; }
    public required long SellerFees { get; init; }
    public required long SellerCinders { get; init; }
    public InventoryItemDto? RemainingInventoryItem { get; init; }
    public required MarketplaceChangeSetDto Marketplace { get; init; }
}
