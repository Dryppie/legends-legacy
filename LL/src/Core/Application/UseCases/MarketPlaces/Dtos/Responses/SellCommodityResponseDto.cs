using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed record SellCommodityResponseDto(
    int FilledQuantity,
    long TotalPrice,
    long SellerFees,
    long SellerCinders,
    InventoryItemDto? RemainingInventoryItem,
    MarketplaceChangeSetDto Marketplace);
