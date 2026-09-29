using API.LiveOps.Previews;
using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.SaveCompensationPackage;
using Application.UseCases.Administration.Commands.GrantCompensationPackage;
using Application.UseCases.Administration.Queries.ListCompensationPackages;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.LiveOps.Controllers;

[Route("api/liveops/compensation-packages")]
[Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
public sealed class CompensationPackagesController(LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record GrantRequest(Guid OperationId, Guid CharacterId, Guid PackageId, int Version, string Reason, string? InternalNotes, Guid PreviewToken = default);
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await Mediator.Send(new ListCompensationPackagesQuery(), ct));
    [HttpPost]
    [API.LiveOps.Operations.TrackOperation("save-package", "PackageId", "package")]
    public async Task<IActionResult> Save([FromBody] CompensationPackageEdit edit, CancellationToken ct)
    {
        var result = await Mediator.Send(new SaveCompensationPackageCommand(edit, CurrentActor), ct);
        return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
    }
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] GrantRequest r, CancellationToken ct)
    {
        var result = await previews.CreatePackageGrantAsync(r.OperationId, r.CharacterId, r.PackageId, r.Version, CurrentActor, r.Reason, r.InternalNotes, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    [HttpPost("grant")]
    [API.LiveOps.Operations.TrackOperation("package", "CharacterId")]
    public async Task<IActionResult> Grant([FromBody] GrantRequest r, CancellationToken ct)
    {
        var validation = await previews.BeginPackageGrantAsync(r.PreviewToken, r.OperationId, r.CharacterId, r.PackageId,
            r.Version, CurrentActor, r.Reason, r.InternalNotes, ct);
        if (!validation.IsSuccess) return validation.IsConflict ? Conflict(Response<bool>.Fail(validation.ErrorMessage)) : BadRequest(Response<bool>.Fail(validation.ErrorMessage));
        try
        {
            var result = await Mediator.Send(new GrantCompensationPackageCommand(r.OperationId, r.CharacterId, r.PackageId,
                r.Version, CurrentActor, r.Reason, r.InternalNotes), ct);
            await previews.CompleteAsync(r.PreviewToken, result.IsSuccess, ct);
            return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
        }
        catch (CompensationPackageConflictException error)
        {
            await previews.CompleteAsync(r.PreviewToken, false, ct);
            return Conflict(Response<bool>.Conflict(error.Message, "package_grant_conflict"));
        }
    }
}
