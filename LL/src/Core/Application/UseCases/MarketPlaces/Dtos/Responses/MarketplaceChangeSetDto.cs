namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed record MarketplaceListingChangeDto(
    Guid ListingId,
    MarketPlaceListingDto? Listing);

public sealed record MarketplaceBuyOrderChangeDto(
    Guid BuyOrderId,
    MarketPlaceBuyOrderDto? BuyOrder);

public sealed class MarketplaceChangeSetDto
{
    public required long Version { get; init; }
    public required IReadOnlyList<MarketplaceListingChangeDto> ListingChanges { get; init; }
    public required IReadOnlyList<MarketplaceBuyOrderChangeDto> BuyOrderChanges { get; init; }
    public required IReadOnlyList<MarketPlaceOrderDto> Orders { get; init; }
    public required IReadOnlyList<Guid> AffectedCharacterIds { get; init; }
}
