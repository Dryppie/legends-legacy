using Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceBuyOrders;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceListings;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class MarketPlaceController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<List<MarketPlaceListingDto>>>> Get() =>
        await Mediator.Send(new GetMarketPlaceListingsQuery());

    [HttpGet("BuyOrders")]
    public async Task<ActionResult<Response<List<MarketPlaceBuyOrderDto>>>> GetBuyOrders() =>
        await Mediator.Send(new GetMarketPlaceBuyOrdersQuery());

    [HttpPost("CreateListing")]
    public async Task<ActionResult<Response<CreateMarketPlaceListingResponseDto>>> CreateListing([FromBody] CreateMarketPlaceListingRequest createMarketPlaceRequest) =>
        await Mediator.Send(new CreateMarketPlaceListingCommand(CurrentCharacterGuid, createMarketPlaceRequest));

    [HttpPost("BuyoutListing")]
    public async Task<ActionResult<Response<BuyoutMarketPlaceListingResponseDto>>> BuyoutListing([FromBody] BuyoutMarketPlaceListingRequest buyoutMarketPlaceRequest) =>
        await Mediator.Send(new BuyoutMarketPlaceListingCommand(CurrentCharacterGuid, buyoutMarketPlaceRequest));

    [HttpPost("CreateBuyOrder")]
    public async Task<ActionResult<Response<CreateMarketPlaceBuyOrderResponseDto>>> CreateBuyOrder([FromBody] CreateMarketPlaceBuyOrderRequest createBuyOrderRequest) =>
        await Mediator.Send(new CreateMarketPlaceBuyOrderCommand(CurrentCharacterGuid, createBuyOrderRequest));

    [HttpPost("FulfillBuyOrder")]
    public async Task<ActionResult<Response<FulfillMarketPlaceBuyOrderResponseDto>>> FulfillBuyOrder([FromBody] FulfillMarketPlaceBuyOrderRequest fulfillBuyOrderRequest) =>
        await Mediator.Send(new FulfillMarketPlaceBuyOrderCommand(CurrentCharacterGuid, fulfillBuyOrderRequest));

    [HttpPost("CancelListing")]
    public async Task<ActionResult<Response<CancelMarketPlaceListingResponseDto>>> CancelListing([FromBody] string listingId) =>
        await Mediator.Send(new CancelMarketPlaceListingCommand(CurrentCharacterGuid, listingId));

    [HttpPost("CancelBuyOrder")]
    public async Task<ActionResult<Response<CancelMarketPlaceBuyOrderResponseDto>>> CancelBuyOrder([FromBody] string buyOrderId) =>
        await Mediator.Send(new CancelMarketPlaceBuyOrderCommand(CurrentCharacterGuid, buyOrderId));
}
