using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.MarketPlaces;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;
public class MarketPlaceListingDto : IMapFrom<MarketPlaceListing>
{
    public void Mapping(Profile profile)
    {
        profile.CreateMap<MarketPlaceListing, MarketPlaceListingDto>();
    }
}
