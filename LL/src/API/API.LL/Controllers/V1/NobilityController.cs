using Application.UseCases.Nobility.Commands.RedeemSignets;
using Application.UseCases.Nobility.Commands.SetNobilityAppearance;
using Application.UseCases.Nobility.Queries.GetNobilityStatus;
using Application.UseCases.Nobility.Queries.PreviewSignetRedemption;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public sealed class NobilityController : BaseController
{
    public sealed record PreviewRequest(int Quantity);
    public sealed record RedeemRequest(Guid OperationId, Guid MembershipVersion, Guid[] UnitIds, DateOnly ExpectedExpiryDate);
    public sealed record AppearanceRequest(bool ShowBadge);
    public sealed record CheckoutRequest(string ProductId);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, await Mediator.Send(
            new Application.UseCases.Nobility.Queries.CreateSignetCheckout.CreateSignetCheckoutQuery(CurrentUserId, CurrentCharacterGuid, request.ProductId), ct));

    [HttpGet("characters/{characterId:guid}")]
    public async Task<IActionResult> PublicAppearance(Guid characterId, CancellationToken ct) =>
        Ok(await Mediator.Send(new Application.UseCases.Nobility.Queries.GetNobilityAppearance.GetNobilityAppearanceQuery(characterId), ct));

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetNobilityStatusQuery(CurrentUserId, CurrentCharacterGuid), ct));

    [HttpPost("redemption-preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request, CancellationToken ct) =>
        Result(await Mediator.Send(new PreviewSignetRedemptionQuery(CurrentUserId, CurrentCharacterGuid, request.Quantity), ct));

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemRequest request, CancellationToken ct) =>
        Result(await Mediator.Send(new RedeemSignetsCommand(CurrentUserId, CurrentCharacterGuid,
            request.OperationId, request.MembershipVersion, request.UnitIds, request.ExpectedExpiryDate), ct));

    [HttpPut("appearance")]
    public async Task<IActionResult> Appearance([FromBody] AppearanceRequest request, CancellationToken ct) =>
        Result(await Mediator.Send(new SetNobilityAppearanceCommand(CurrentUserId, CurrentCharacterGuid,
            request.ShowBadge), ct));

    private IActionResult Result<T>(Response<T> result) => result.IsSuccess ? Ok(result) :
        result.IsConflict ? Conflict(result) : BadRequest(result);
}
