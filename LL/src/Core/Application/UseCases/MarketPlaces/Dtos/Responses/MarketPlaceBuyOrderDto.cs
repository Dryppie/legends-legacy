using Application.Common.Mappings;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.MarketPlaces;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public class MarketPlaceBuyOrderDto : IMapFrom<MarketPlaceBuyOrder>
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBaseDto ItemBase { get; set; } = null!;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<MarketPlaceBuyOrder, MarketPlaceBuyOrderDto>();
    }
}
