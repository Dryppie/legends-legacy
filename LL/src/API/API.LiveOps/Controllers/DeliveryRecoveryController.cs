using API.LiveOps.Previews;
using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.RetryStateRefreshDelivery;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;
[Route("api/liveops/deliveries/{deliveryId:guid}/state-refresh")]
[Authorize(Policy = AdministrationPermissions.SuperAdmin)]
public sealed class DeliveryRecoveryController(LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record RetryRequest(Guid OperationId, string Reason, Guid PreviewToken = default);
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(Guid deliveryId, [FromBody] RetryRequest request, CancellationToken ct)
    {
        var result = await previews.CreateStateRefreshRecoveryAsync(request.OperationId, deliveryId, CurrentActor, request.Reason, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    [HttpPost]
    [Operations.TrackOperation("delivery-retry", "deliveryId", "delivery")]
    public async Task<IActionResult> Retry(Guid deliveryId, [FromBody] RetryRequest request, CancellationToken ct)
    {
        var validation = await previews.BeginStateRefreshRecoveryAsync(request.PreviewToken, request.OperationId, deliveryId, CurrentActor, request.Reason, ct);
        if (!validation.IsSuccess) return validation.IsConflict ? Conflict(Response<bool>.Fail(validation.ErrorMessage)) : BadRequest(Response<bool>.Fail(validation.ErrorMessage));
        var result = await Mediator.Send(new RetryStateRefreshDeliveryCommand(request.OperationId, deliveryId, CurrentActor, request.Reason), ct);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, ct);
        return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
    }
}
