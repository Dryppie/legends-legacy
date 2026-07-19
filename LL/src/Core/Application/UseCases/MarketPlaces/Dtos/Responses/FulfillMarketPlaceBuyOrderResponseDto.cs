using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class FulfillMarketPlaceBuyOrderResponseDto : IMapFrom<FulfillMarketPlaceBuyOrderResult>
{
    public required Guid BuyOrderId { get; init; }
    public MarketPlaceBuyOrderDto? RemainingBuyOrder { get; init; }
    public required InventoryItemDto PurchasedItem { get; init; }
    public InventoryItemDto? RemainingSellerInventoryItem { get; init; }
    public required Guid SoldItemInstanceId { get; init; }
    public required int SoldQuantity { get; init; }
    public required long TotalPrice { get; init; }
    public required long SellerFee { get; init; }
    public required long SellerCinders { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<FulfillMarketPlaceBuyOrderResult, FulfillMarketPlaceBuyOrderResponseDto>()
            .ForMember(destination => destination.SoldItemInstanceId,
                options => options.MapFrom(source => source.PurchasedItem.ItemInstanceId))
            .ForMember(destination => destination.SoldQuantity,
                options => options.MapFrom(source => source.Quantity));
    }
}
