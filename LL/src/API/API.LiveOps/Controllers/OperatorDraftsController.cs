using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.SaveOperatorDraft;
using Application.UseCases.Administration.Queries.GetOperatorDraft;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/drafts")]
[Authorize(Policy = AdministrationPermissions.Read)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(32768)]
public sealed class OperatorDraftsController : LiveOpsControllerBase
{
    public sealed record SaveRequest(Guid ExpectedVersion, string Content);
    [HttpGet("workspace")]
    public async Task<IActionResult> Workspace(CancellationToken ct) => Ok(await Mediator.Send(new GetOperatorDraftQuery(CurrentActor.Subject, "workspace"), ct));
    [HttpPost("workspace")]
    public Task<IActionResult> SaveWorkspace(SaveRequest request, CancellationToken ct) => Save("workspace", request, ct);
    [HttpGet("{scope}/{target:guid}")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<IActionResult> Get(string scope, Guid target, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOperatorDraftQuery(CurrentActor.Subject, $"{scope}:{target:D}"), ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    [HttpPost("{scope}/{target:guid}")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    [RequestSizeLimit(32768)]
    public Task<IActionResult> SaveCase(string scope, Guid target, SaveRequest request, CancellationToken ct) => Save($"{scope}:{target:D}", request, ct);
    private async Task<IActionResult> Save(string key, SaveRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SaveOperatorDraftCommand(CurrentActor.Subject, key, request.ExpectedVersion, request.Content), ct);
        return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
    }
}
