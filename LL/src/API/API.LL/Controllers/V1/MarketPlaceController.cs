using Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.BuyCommodity;
using Application.UseCases.MarketPlaces.Commands.SellCommodity;
using Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
using Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceBuyOrders;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceListings;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceCatalog;
using Application.UseCases.Items.Dtos;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceOrderHistory;
using Application.UseCases.MarketPlaces.Queries.GetMarketPlaceItemSummary;
using API.LL.Common;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize(Policy = AuthorizationPolicies.RegisteredUser)]
public class MarketPlaceController : BaseController
{
    [HttpGet("Catalog")]
    public async Task<ActionResult<Response<List<ItemBaseDto>>>> GetCatalog() =>
        await Mediator.Send(new GetMarketPlaceCatalogQuery());

    [HttpGet("History")]
    public async Task<ActionResult<Response<List<MarketPlaceOrderDto>>>> GetHistory([FromQuery] int take = 50) =>
        await Mediator.Send(new GetMarketPlaceOrderHistoryQuery(CurrentCharacterGuid, take));

    [HttpGet("Summary/{itemBaseId}")]
    public async Task<ActionResult<Response<MarketPlaceItemSummaryDto>>> GetSummary(string itemBaseId) =>
        await Mediator.Send(new GetMarketPlaceItemSummaryQuery(itemBaseId));

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

    [HttpPost("BuyCommodity")]
    public async Task<ActionResult<Response<BuyCommodityResponseDto>>> BuyCommodity([FromBody] BuyCommodityRequest request) =>
        await Mediator.Send(new BuyCommodityCommand(CurrentCharacterGuid, request));

    [HttpPost("SellCommodity")]
    public async Task<ActionResult<Response<SellCommodityResponseDto>>> SellCommodity([FromBody] SellCommodityRequest request) =>
        await Mediator.Send(new SellCommodityCommand(CurrentCharacterGuid, request));

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
