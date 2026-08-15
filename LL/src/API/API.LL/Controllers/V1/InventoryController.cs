using Application.UseCases.Inventories.Commands.ScrapEquipments;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Commands.OpenCatalystSelectionCrate;
using Application.UseCases.Inventories.Commands.TransferInventoryItem;
using Application.UseCases.Inventories.Queries.GetInventoryById;
using API.LL.Common;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]

public class InventoryController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<InventoryDto>>> Get() =>
        await Mediator.Send(new GetInventoryByIdQuery(CurrentCharacterGuid));

    [HttpPost("Scrap")]
    public async Task<ActionResult<Response<ScrapEquipmentsResponseDto>>> Scrap([FromBody] List<string> itemIds) =>
        await Mediator.Send(new ScrapEquipmentsCommand(CurrentCharacterGuid, itemIds));

    [HttpPost("items/{crateItemInstanceId:guid}/open-catalyst-selection-crate")]
    [HttpPost("items/{crateItemInstanceId:guid}/open-selection-container")]
    public async Task<ActionResult<Response<OpenSelectionCrateResultDto>>> OpenSelectionContainer(
        Guid crateItemInstanceId,
        [FromBody] OpenSelectionCrateRequestDto request) =>
        await Mediator.Send(new OpenCatalystSelectionCrateCommand(
            CurrentCharacterGuid,
            crateItemInstanceId,
            request.OptionId));

    [HttpPost("items/{itemInstanceId:guid}/transfer")]
    [Authorize(Policy = AuthorizationPolicies.RegisteredUser)]
    public async Task<ActionResult<Response<TransferInventoryItemResponseDto>>> Transfer(
        Guid itemInstanceId,
        [FromBody] TransferInventoryItemRequestDto request) =>
        await Mediator.Send(new TransferInventoryItemCommand(
            CurrentCharacterGuid,
            itemInstanceId,
            request.RecipientName,
            request.Quantity));
}
