using Common.Primitives;
using Domain.Models.Administration;

namespace API.LiveOps.Previews;
public sealed partial class LiveOpsActionPreviewService
{
    public async Task<Response<ActionPreviewDto>> CreateStateRefreshRecoveryAsync(Guid operationId, Guid deliveryId,
        AdministrationActor actor, string reason, CancellationToken ct)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null || reason.Trim().Length < 3)
            return Response<ActionPreviewDto>.Fail(validation ?? "Supply a reason of at least three characters.");
        var prepared = await recovery!.PrepareAsync(operationId, deliveryId, actor, ct);
        if (!prepared.IsSuccess || prepared.Data is null) return Response<ActionPreviewDto>.Fail(prepared.ErrorMessage);
        var plan = prepared.Data;
        return await PersistAsync(operationId, AdminActionPreviewKinds.StateRefreshRecovery, actor, deliveryId,
            RefreshRequestHash(deliveryId, reason), StateHash(plan), new PreviewContext(plan.CharacterId, null),
            "Retry player state refresh", plan.CharacterName, "Normal", null,
            [new("Player", plan.CharacterName), new("Failed delivery", deliveryId.ToString()),
             new("Original message", plan.MessageId.ToString()), new("Failed attempts", plan.FailedAttempts.ToString()),
             new("State to refresh", plan.Scopes), new("Effect", "Queue one replacement notification using the retained state revision."),
             new("Reason", reason.Trim())],
            ["No inventory, currency, rewards or restrictions are changed. The original failed delivery is retained for diagnosis.",
             "The Game worker uses its normal retry policy. Queueing or processing does not prove client receipt; an offline player may refresh on reconnect."], ct);
    }
    public Task<PreviewSubmissionResult> BeginStateRefreshRecoveryAsync(Guid token, Guid operationId, Guid deliveryId,
        AdministrationActor actor, string reason, CancellationToken ct) => BeginAsync(token, operationId,
            AdminActionPreviewKinds.StateRefreshRecovery, deliveryId, actor, RefreshRequestHash(deliveryId, reason), ct);
    private static string RefreshRequestHash(Guid deliveryId, string reason) =>
        RequestHash(AdminActionPreviewKinds.StateRefreshRecovery, new { DeliveryId = deliveryId, Reason = NormalizeRequired(reason) });
}
