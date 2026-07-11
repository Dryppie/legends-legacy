using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using AutoMapper;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CreateMarketPlaceBuyOrderResponseDto : IMapFrom<CreateMarketPlaceBuyOrderResult>
{
    public required MarketPlaceBuyOrderDto BuyOrder { get; init; }
    public required long BuyerCinders { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreateMarketPlaceBuyOrderResult, CreateMarketPlaceBuyOrderResponseDto>();
    }
}
