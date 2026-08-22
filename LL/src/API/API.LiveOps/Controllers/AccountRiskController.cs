using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.AddAccountRiskNote;
using Application.UseCases.Administration.Commands.UpdateAccountRiskStatus;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.GetAccountRiskDetails;
using Application.UseCases.Administration.Queries.GetAccountRiskPage;
using Application.UseCases.Administration.Queries.GetAccountTemporalCorrelations;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.LiveOps.Support;

namespace API.LiveOps.Controllers;

[Route("api/liveops/account-risk")]
public sealed class AccountRiskController(
    TransferConversationCorrelationService transferConversationCorrelations,
    ILogger<AccountRiskController> logger)
    : LiveOpsControllerBase
{
    public sealed record UpdateStatusRequest(Guid OperationId, AccountInvestigationStatus Status, string Reason);
    public sealed record AddNoteRequest(Guid OperationId, string Note);

    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<AccountRiskPageDto>>> Search(
        [FromQuery] string? search = null,
        [FromQuery] AccountRiskSeverity? minimumSeverity = AccountRiskSeverity.Low,
        [FromQuery] AccountRiskSignalType? signalType = null,
        [FromQuery] AccountInvestigationStatus? status = null,
        [FromQuery] int? minimumScore = null,
        [FromQuery] int? maximumAccountAgeDays = null,
        [FromQuery] DateTimeOffset? lastTriggeredAfter = null,
        [FromQuery] string sort = "risk",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetAccountRiskPageQuery(
            search,
            minimumSeverity,
            signalType,
            status,
            minimumScore,
            maximumAccountAgeDays,
            lastTriggeredAfter,
            sort,
            page,
            pageSize));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{accountId:guid}")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<AccountRiskDetailsDto>>> GetDetails(
        Guid accountId,
        [FromQuery] int transferLimit = 100)
    {
        var result = await Mediator.Send(new GetAccountRiskDetailsQuery(accountId, transferLimit));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("{accountId:guid}/temporal-correlations")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<AccountTemporalCorrelationReportDto>>> GetTemporalCorrelations(
        Guid accountId,
        [FromQuery] int? windowDays = null)
    {
        var result = await Mediator.Send(new GetAccountTemporalCorrelationsQuery(accountId, windowDays));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("{accountId:guid}/transfer-conversation-correlations")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<TransferConversationCorrelationReportDto>>>
        GetTransferConversationCorrelations(
            Guid accountId,
            CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Operator {OperatorSubject} accessed transfer conversation correlations for account {AccountId}.",
            CurrentActor.Subject,
            accountId);
        var result = await transferConversationCorrelations.GetAsync(
            accountId,
            cancellationToken);
        return result.AccountFound && result.Report is not null
            ? Ok(Response<TransferConversationCorrelationReportDto>.Success(result.Report))
            : NotFound(Response<TransferConversationCorrelationReportDto>.Fail(
                "The account was not found."));
    }

    [HttpPost("{accountId:guid}/status")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountRiskOperationDto>>> UpdateStatus(
        Guid accountId,
        [FromBody] UpdateStatusRequest request)
    {
        var result = await Mediator.Send(new UpdateAccountRiskStatusCommand(
            request.OperationId,
            accountId,
            request.Status,
            CurrentActor,
            request.Reason));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{accountId:guid}/notes")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountRiskOperationDto>>> AddNote(
        Guid accountId,
        [FromBody] AddNoteRequest request)
    {
        var result = await Mediator.Send(new AddAccountRiskNoteCommand(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Note));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
