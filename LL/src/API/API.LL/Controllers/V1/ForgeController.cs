using Application.UseCases.Equipments.Commands.ChangeEquipmentProgressionStyle;
using Application.UseCases.Equipments.Commands.ImproveEquipmentProgressionRank;
using Application.UseCases.Equipments.Commands.LearnEquipmentProgressionStyle;
using Application.UseCases.Equipments.Commands.SalvageEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Queries.GetForgeStyles;
using Application.UseCases.Equipments.Queries.PreviewForge;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public sealed class ForgeController : BaseController
{
    public sealed record RankRequest(Guid OperationId, Guid ItemInstanceId, string QuoteToken);
    public sealed record StyleRequest(Guid OperationId, Guid ItemInstanceId, string? StyleId, string QuoteToken);
    public sealed record LearnRequest(Guid OperationId, Guid ItemInstanceId, string StyleId, string QuoteToken);
    public sealed record SalvageRequest(Guid OperationId, Guid ItemInstanceId, bool AllowFavoriteSalvage, string QuoteToken);

    [HttpPost("preview")]
    public async Task<ActionResult<ForgeQuoteDto>> Preview([FromBody] ForgeRequest request, CancellationToken ct) =>
        await Mediator.Send(new PreviewForgeQuery(CurrentCharacterGuid, request), ct);

    [HttpGet("styles")]
    public async Task<ActionResult<IReadOnlyList<ForgeStyleOptionDto>>> Styles([FromQuery] Guid itemInstanceId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetForgeStylesQuery(CurrentCharacterGuid, itemInstanceId), ct));

    [HttpPost("rank")]
    public async Task<ActionResult<Response<ForgeMutationDto>>> Rank([FromBody] RankRequest request, CancellationToken ct) =>
        await Mediator.Send(new ImproveEquipmentProgressionRankCommand(CurrentCharacterGuid, request.OperationId, request.ItemInstanceId, request.QuoteToken), ct);

    [HttpPost("style")]
    public async Task<ActionResult<Response<ForgeMutationDto>>> Style([FromBody] StyleRequest request, CancellationToken ct) =>
        await Mediator.Send(new ChangeEquipmentProgressionStyleCommand(CurrentCharacterGuid, request.OperationId, request.ItemInstanceId, request.StyleId, request.QuoteToken), ct);

    [HttpPost("learn")]
    public async Task<ActionResult<Response<ForgeMutationDto>>> Learn([FromBody] LearnRequest request, CancellationToken ct) =>
        await Mediator.Send(new LearnEquipmentProgressionStyleCommand(CurrentCharacterGuid, request.OperationId, request.ItemInstanceId, request.StyleId, request.QuoteToken), ct);

    [HttpPost("salvage")]
    public async Task<ActionResult<Response<ForgeMutationDto>>> Salvage([FromBody] SalvageRequest request, CancellationToken ct) =>
        await Mediator.Send(new SalvageEquipmentCommand(CurrentCharacterGuid, request.OperationId, request.ItemInstanceId, request.AllowFavoriteSalvage, request.QuoteToken), ct);
}
