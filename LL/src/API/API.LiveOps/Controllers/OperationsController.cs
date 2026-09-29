using Application.UseCases.Administration;
using Application.Interfaces.Services.LL.Administration;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/operations")]
[Authorize(Policy = AdministrationPermissions.Read)]
public sealed class OperationsController(IOperatorOperationRepository operations, IChatModerationGateway chat, IHostEnvironment environment, TimeProvider time) : LiveOpsControllerBase
{
    public sealed record ChatEnforcementStatus(string State, Guid? RestrictionId, DateTimeOffset? ExpiresAt, DateTimeOffset CheckedAt, string Message);
    public sealed record OperationStatus(OperatorOperation Operation, OperationDeliverySummary? Delivery, string Coverage, ChatEnforcementStatus? Chat = null);
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] int page = 1, [FromQuery] bool unresolvedOnly = true, CancellationToken ct = default) =>
        Ok(Response<OperatorOperationPage>.Success(await operations.SearchAsync(CurrentActor.Subject, environment.EnvironmentName, page, unresolvedOnly, ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var actor = CurrentActor.Subject;
        var operation = await operations.GetAsync(actor, environment.EnvironmentName, id, ct);
        if (operation is null) return NotFound(Response<OperationStatus>.Fail("No received-operation record for this operator and environment. Older operations may still have an audit receipt."));
        var coverage = "Game outbox records are retained for a bounded period. Processed means the consumer completed, not that the player's client acknowledged the update. No retained rows does not establish that no delivery was required.";
        ChatEnforcementStatus? chatStatus = null;
        if (operation.Source == "Chat") {
            coverage = "Chat has no operation-specific delivery acknowledgement contract. Its committed moderation receipt is separate from client delivery.";
            if (operation.Outcome != "Committed") {
                try {
                    var receipt = await chat.GetAuditAsync(new(null, null, null, actor, null, id, [], null, null, null, 10), ct);
                    if (receipt.IsSuccess && receipt.Entries.Any(x => x.OperationId == id && x.ActorSubject == actor)) {
                        operation.Outcome = "Committed"; await operations.FinishAsync(actor, environment.EnvironmentName, id, "Committed", ct);
                    } else if (!receipt.IsSuccess) coverage += " Chat receipt lookup is unavailable; the outcome is not confirmed.";
                } catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch { coverage += " Chat receipt lookup is unavailable; the outcome is not confirmed."; }
            }
            try {
                var current = await chat.GetStateAsync(operation.TargetId, 1, ct);
                chatStatus = current.IsSuccess
                    ? new(current.ActiveMute is null ? "No active mute" : "Muted", current.ActiveMute?.Id, current.ActiveMute?.ExpiresAt, time.GetUtcNow(),
                        "Current server restriction state. A later action or expiry can change it; it does not establish whether this particular request committed or a client received an update.")
                    : new("Unavailable", null, null, time.GetUtcNow(), "Current Chat restriction state could not be read. The operation receipt remains separate.");
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { chatStatus = new("Unavailable", null, null, time.GetUtcNow(), "Current Chat restriction state could not be read. The operation receipt remains separate."); }
        }
        // Only an owned committed operation may expose its correlated Game delivery rows.
        var delivery = operation.Source == "Game" && operation.Outcome == "Committed" ? await operations.DeliveriesAsync(id, ct) : null;
        return Ok(Response<OperationStatus>.Success(new(operation, delivery, coverage, chatStatus)));
    }
}
