using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace Services.LL.Administration;

public sealed class StateRefreshRecoveryService(IStateRefreshRecoveryRepository repository,
    IAdministrationRepository administration, JsonSerializerOptions json, TimeProvider time) : IStateRefreshRecoveryService
{
    public async Task<Response<StateRefreshRecoveryPlan>> PrepareAsync(Guid operationId, Guid deliveryId, AdministrationActor actor, CancellationToken ct)
    {
        if (operationId == Guid.Empty || deliveryId == Guid.Empty || string.IsNullOrWhiteSpace(actor.Subject))
            return Response<StateRefreshRecoveryPlan>.Fail("An operation, delivery and operator are required.");
        var existing = await administration.GetActionAsync(operationId, ct);
        if (existing is not null)
        {
            var receipt = ReadReceipt(existing);
            return receipt is not null && existing.ActorSubject == actor.Subject && receipt.Plan.DeliveryId == deliveryId
                ? Response<StateRefreshRecoveryPlan>.Success(receipt.Plan)
                : Response<StateRefreshRecoveryPlan>.Conflict("This operation reference belongs to another request.", "recovery_reference_conflict");
        }
        var previous = await repository.PreviousRepairAsync(deliveryId, ct);
        if (previous is not null)
            return Response<StateRefreshRecoveryPlan>.Conflict($"A replacement was already queued by operation {previous.Id:D}. Inspect that operation's delivery instead.", "recovery_already_queued");
        var delivery = await repository.GetAsync(deliveryId, ct);
        if (delivery?.Status != GameEventOutboxDeliveryStatus.Failed || delivery.Consumer != GameEventOutboxConsumerNames.RealtimeDelivery ||
            delivery.Message.EventType != GameEventTypes.RealtimeDeliveryRequested)
            return Response<StateRefreshRecoveryPlan>.Fail("Only a retained, failed player state-refresh notification can be retried here. Pending, processing, processed and gameplay consumers are not eligible.");
        var payload = ReadRefresh(delivery.Message);
        if (payload is null)
            return Response<StateRefreshRecoveryPlan>.Fail("This delivery is not a valid state-refresh notification for one player. No repair was prepared.");
        var player = await administration.GetPlayerByCharacterIdAsync(payload.Value.CharacterId, time.GetUtcNow(), ct);
        if (player is null) return Response<StateRefreshRecoveryPlan>.Fail("The target player is no longer available.");
        return Response<StateRefreshRecoveryPlan>.Success(new(delivery.Id, delivery.MessageId, player.CharacterId,
            player.AccountId, player.CharacterName, delivery.Message.AdministrationOperationId, delivery.Attempts,
            delivery.CreatedAt, payload.Value.Scopes, Hash(delivery.Message.PayloadJson)));
    }

    public async Task<Response<StateRefreshRecoveryResult>> QueueAsync(Guid operationId, Guid deliveryId, AdministrationActor actor, string reason, CancellationToken ct)
    {
        reason = reason?.Trim() ?? "";
        if (reason.Length is < 3 or > 1000 || actor.Subject.Length is < 1 or > 320 || actor.DisplayName.Length > 320)
            return Response<StateRefreshRecoveryResult>.Fail("Supply a reason of 3–1000 characters and a valid operator.");
        await repository.LockAsync(operationId, deliveryId, ct);
        var hash = Hash(JsonSerializer.Serialize(new { deliveryId, reason }));
        var existing = await administration.GetActionAsync(operationId, ct);
        if (existing is not null)
        {
            var receipt = ReadReceipt(existing);
            return receipt?.RequestHash == hash && existing.ActorSubject == actor.Subject
                ? Response<StateRefreshRecoveryResult>.Success(new(operationId, deliveryId, receipt.Plan.CharacterId, receipt.ReplacementMessageId, true))
                : Response<StateRefreshRecoveryResult>.Conflict("This operation reference belongs to another request.", "recovery_reference_conflict");
        }
        var prepared = await PrepareAsync(operationId, deliveryId, actor, ct);
        if (!prepared.IsSuccess || prepared.Data is null)
            return Response<StateRefreshRecoveryResult>.Conflict(prepared.ErrorMessage, "recovery_state_changed");
        var plan = prepared.Data;
        var original = await repository.GetAsync(deliveryId, ct);
        if (original?.Status != GameEventOutboxDeliveryStatus.Failed || Hash(original.Message.PayloadJson) != plan.Fingerprint)
            return Response<StateRefreshRecoveryResult>.Conflict("The retained delivery changed. Review it again.", "recovery_state_changed");
        var now = time.GetUtcNow(); var messageId = Guid.NewGuid();
        // Preserve the failed row for diagnosis. The replacement has a new update ID,
        // but the same scoped revision: clients fetch current state and never replay rewards.
        repository.AddReplacement(new GameEventOutboxMessage { Id = messageId, CharacterId = plan.CharacterId,
            AccountId = plan.AccountId, EventType = GameEventTypes.RealtimeDeliveryRequested,
            PayloadJson = original.Message.PayloadJson, CreatedAt = now, AvailableAt = now,
            AdministrationOperationId = operationId,
            Deliveries = [new GameEventOutboxDelivery { Id = Guid.NewGuid(), MessageId = messageId,
                Consumer = GameEventOutboxConsumerNames.RealtimeDelivery, CreatedAt = now, AvailableAt = now }] });
        administration.AddAction(new AdminAction { Id = operationId, ActionType = AdminActionType.StateRefreshDeliveryRetried,
            Permission = AdministrationPermissions.SuperAdmin, ActorSubject = actor.Subject, ActorDisplayName = actor.DisplayName,
            TargetAccountId = plan.AccountId, TargetCharacterId = plan.CharacterId, TargetResourceId = deliveryId,
            Reason = reason, OccurredAt = now, DetailsJson = JsonSerializer.Serialize(new Receipt(hash, plan, messageId)) });
        return Response<StateRefreshRecoveryResult>.Success(new(operationId, deliveryId, plan.CharacterId, messageId, false));
    }

    private (Guid CharacterId, string Scopes)? ReadRefresh(GameEventOutboxMessage message)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<RealtimeDeliveryRequestedPayload>(message.PayloadJson, json);
            if (envelope?.Audience is not { Kind: "character", TargetId: { } characterId } || characterId == Guid.Empty ||
                message.CharacterId != characterId || envelope.Audience.CharacterIds?.Count > 0) return null;
            if (envelope.EventName == GameRealtimeEventNames.StateInvalidated)
            {
                var payload = envelope.Payload.Deserialize<StateInvalidated>(json);
                return payload?.CharacterId == characterId && payload.Revision > 0 && StateSyncScopes.CharacterResources.Contains(payload.Scope)
                    ? (characterId, $"{payload.Scope}: revision {payload.Revision}") : null;
            }
            if (envelope.EventName == GameRealtimeEventNames.StateInvalidations)
            {
                var payload = envelope.Payload.Deserialize<StateInvalidations>(json);
                return payload?.CharacterId == characterId && payload.Revisions is { Count: > 0 and <= 32 } &&
                    payload.Revisions.All(x => x.Value > 0 && StateSyncScopes.CharacterResources.Contains(x.Key))
                    ? (characterId, string.Join(", ", payload.Revisions.OrderBy(x => x.Key).Select(x => $"{x.Key}: revision {x.Value}"))) : null;
            }
        }
        catch (JsonException) { }
        return null;
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Receipt? ReadReceipt(AdminAction action) => action.ActionType == AdminActionType.StateRefreshDeliveryRetried
        ? JsonSerializer.Deserialize<Receipt>(action.DetailsJson) : null;
    private sealed record Receipt(string RequestHash, StateRefreshRecoveryPlan Plan, Guid ReplacementMessageId);
}
