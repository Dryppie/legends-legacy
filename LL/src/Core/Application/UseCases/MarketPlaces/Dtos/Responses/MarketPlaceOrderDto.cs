using Application.Common.Mappings;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.MarketPlaces;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class MarketPlaceOrderDto : IMapFrom<MarketPlaceOrder>
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public Guid? ItemInstanceId { get; set; }
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public long TotalPrice { get; set; }
    public long SellerFee { get; set; }
    public MarketPlaceTradeSource Source { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<MarketPlaceOrder, MarketPlaceOrderDto>();
}
