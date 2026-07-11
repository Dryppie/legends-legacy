using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using AutoMapper;

namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed class CancelMarketPlaceBuyOrderResponseDto : IMapFrom<CancelMarketPlaceBuyOrderResult>
{
    public required Guid BuyOrderId { get; init; }
    public required long BuyerCinders { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CancelMarketPlaceBuyOrderResult, CancelMarketPlaceBuyOrderResponseDto>();
    }
}
