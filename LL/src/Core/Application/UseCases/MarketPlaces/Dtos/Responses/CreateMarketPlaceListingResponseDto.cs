using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CreateMarketPlaceListingResponseDto
{
    public required MarketPlaceListingDto Listing { get; init; }
    public required Guid ListedItemInstanceId { get; init; }
    public required int ListedQuantity { get; init; }
    public InventoryItemDto? RemainingInventoryItem { get; init; }
}
