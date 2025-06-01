using Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceListings;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class MarketPlaceController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<List<MarketPlaceListingDto>>>> Get() =>
        await Mediator.Send(new GetMarketPlaceListingsQuery());

    [HttpPost("CreateListing")]
    public async Task<ActionResult<Response<bool>>> CreateListing([FromBody] CreateMarketPlaceListingRequest createMarketPlaceRequest) =>
        await Mediator.Send(new CreateMarketPlaceListingCommand(CurrentCharacterGuid, createMarketPlaceRequest));

    [HttpPost("BuyoutListing")]
    public async Task<ActionResult<Response<bool>>> BuyoutListing([FromBody] BuyoutMarketPlaceListingRequest buyoutMarketPlaceRequest) =>
        await Mediator.Send(new BuyoutMarketPlaceListingCommand(CurrentCharacterGuid, buyoutMarketPlaceRequest));
}
