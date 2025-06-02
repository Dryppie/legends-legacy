using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.MarketPlaces;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;
public class MarketPlaceListingDto : IMapFrom<MarketPlaceListing>
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public ItemInstanceDto ItemInstance { get; set; } = null!;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<MarketPlaceListing, MarketPlaceListingDto>();
    }
}
