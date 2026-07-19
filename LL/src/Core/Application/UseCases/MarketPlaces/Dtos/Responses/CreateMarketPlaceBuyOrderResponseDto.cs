using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using AutoMapper;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CreateMarketPlaceBuyOrderResponseDto : IMapFrom<CreateMarketPlaceBuyOrderResult>
{
    public MarketPlaceBuyOrderDto? BuyOrder { get; init; }
    public required int FilledQuantity { get; init; }
    public required long FilledTotalPrice { get; init; }
    public required long BuyerCinders { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreateMarketPlaceBuyOrderResult, CreateMarketPlaceBuyOrderResponseDto>();
    }
}
