using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.PlanSupportCaseFollowUp;
using Application.UseCases.Administration.Commands.CreateSupportCase;
using Application.UseCases.Administration.Commands.AddSupportCaseNote;
using Application.UseCases.Administration.Commands.UpdateSupportCaseStatus;
using Application.UseCases.Administration.Commands.LinkSupportCaseOperation;
using Application.UseCases.Administration.Queries.GetSupportCase;
using Application.UseCases.Administration.Queries.SearchSupportCases;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/cases")]
[Authorize(Policy = AdministrationPermissions.AccountModeration)]
public sealed class SupportCasesController : LiveOpsControllerBase
{
    public sealed record CreateRequest(Guid OperationId, Guid CharacterId, string Title, string Category, string Body, string? ExternalReference);
    public sealed record NoteRequest(Guid OperationId, int ExpectedVersion, string Body, string? EvidenceReference);
    public sealed record StatusRequest(Guid OperationId, int ExpectedVersion, SupportCaseStatus Status, string Body);
    public sealed record LinkRequest(Guid OperationId, int ExpectedVersion, Guid LinkedOperationId, string Source, string Body);
    public sealed record FollowUpRequest(Guid OperationId, int ExpectedVersion, SupportCasePriority Priority, DateTimeOffset? FollowUpAt, string NextAction, string Body);
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? characterId, [FromQuery] SupportCaseStatus? status,
        [FromQuery] string? search, [FromQuery] int page = 1, CancellationToken ct = default, [FromQuery] string? category = null, [FromQuery] string sort = "recent", [FromQuery] bool overdue = false) =>
        Ok(await Mediator.Send(new SearchSupportCasesQuery(characterId, status, search, page, category, sort, overdue), ct));
    [HttpGet("{caseId:guid}")]
    public async Task<IActionResult> Get(Guid caseId, [FromQuery] int? beforeSequence, CancellationToken ct) =>
        Result(await Mediator.Send(new GetSupportCaseQuery(caseId, beforeSequence), ct));
    [HttpPost]
    [API.LiveOps.Operations.TrackOperation("case-create", "CharacterId")]
    public async Task<IActionResult> Create([FromBody] CreateRequest r, CancellationToken ct) =>
        Result(await Mediator.Send(new CreateSupportCaseCommand(new(r.OperationId, r.OperationId, r.CharacterId, 0,
            SupportCaseEntryKind.Created, r.Body, r.Category, r.Title, r.ExternalReference), CurrentActor), ct));
    [HttpPost("{caseId:guid}/notes")]
    [API.LiveOps.Operations.TrackOperation("case-notes", "caseId", "case")]
    public async Task<IActionResult> Note(Guid caseId, [FromBody] NoteRequest r, CancellationToken ct) =>
        Result(await Mediator.Send(new AddSupportCaseNoteCommand(new(r.OperationId, caseId, Guid.Empty, r.ExpectedVersion,
            SupportCaseEntryKind.Note, r.Body, EvidenceReference: r.EvidenceReference), CurrentActor), ct));
    [HttpPost("{caseId:guid}/status")]
    [API.LiveOps.Operations.TrackOperation("case-status", "caseId", "case")]
    public async Task<IActionResult> Status(Guid caseId, [FromBody] StatusRequest r, CancellationToken ct) =>
        Result(await Mediator.Send(new UpdateSupportCaseStatusCommand(new(r.OperationId, caseId, Guid.Empty, r.ExpectedVersion,
            SupportCaseEntryKind.StatusChanged, r.Body, Status: r.Status), CurrentActor), ct));
    [HttpPost("{caseId:guid}/operations")]
    [API.LiveOps.Operations.TrackOperation("case-operations", "caseId", "case")]
    public async Task<IActionResult> Link(Guid caseId, [FromBody] LinkRequest r, CancellationToken ct) =>
        Result(await Mediator.Send(new LinkSupportCaseOperationCommand(new(r.OperationId, caseId, Guid.Empty, r.ExpectedVersion,
            SupportCaseEntryKind.OperationLinked, r.Body, LinkedOperationId: r.LinkedOperationId, LinkedSource: r.Source), CurrentActor), ct));
    [HttpPost("{caseId:guid}/follow-up")]
    [API.LiveOps.Operations.TrackOperation("case-follow-up", "caseId", "case")]
    public async Task<IActionResult> FollowUp(Guid caseId, [FromBody] FollowUpRequest r, CancellationToken ct) =>
        Result(await Mediator.Send(new PlanSupportCaseFollowUpCommand(new(r.OperationId, caseId, Guid.Empty, r.ExpectedVersion,
            SupportCaseEntryKind.FollowUpChanged, r.Body, Priority: r.Priority, FollowUpAt: r.FollowUpAt, NextAction: r.NextAction), CurrentActor), ct));
    private IActionResult Result(Response<SupportCaseDetailsDto> result) => result.IsSuccess ? Ok(result) :
        result.IsConflict ? Conflict(result) : result.ErrorCode == "case_not_found" ? NotFound(result) : BadRequest(result);
}
