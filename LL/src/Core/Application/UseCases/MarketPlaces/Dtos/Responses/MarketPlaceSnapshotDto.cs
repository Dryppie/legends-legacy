using Application.UseCases.Items.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed record MarketPlaceSnapshotDto(
    IReadOnlyList<MarketPlaceListingDto> Listings,
    IReadOnlyList<ItemBaseDto> Catalog,
    IReadOnlyList<MarketPlaceOrderDto> History,
    IReadOnlyList<MarketPlaceBuyOrderDto> BuyOrders);
