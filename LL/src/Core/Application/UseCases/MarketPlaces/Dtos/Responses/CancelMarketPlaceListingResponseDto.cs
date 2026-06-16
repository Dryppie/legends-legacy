using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CancelMarketPlaceListingResponseDto
{
    public required Guid ListingId { get; init; }
    public required InventoryItemDto ReturnedItem { get; init; }
}
