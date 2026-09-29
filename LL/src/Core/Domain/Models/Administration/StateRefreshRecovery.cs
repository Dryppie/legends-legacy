using Domain.Models.Outbox;

namespace Domain.Models.Administration;

public sealed record StateRefreshRecoveryPlan(Guid DeliveryId, Guid MessageId, Guid CharacterId,
    Guid AccountId, string CharacterName, Guid? OriginalOperationId, int FailedAttempts,
    DateTimeOffset CreatedAt, string Scopes, string Fingerprint);
public sealed record StateRefreshRecoveryResult(Guid OperationId, Guid DeliveryId, Guid CharacterId,
    Guid ReplacementMessageId, bool WasAlreadyProcessed);

public interface IStateRefreshRecoveryRepository
{
    Task LockAsync(Guid operationId, Guid deliveryId, CancellationToken ct);
    Task<GameEventOutboxDelivery?> GetAsync(Guid deliveryId, CancellationToken ct);
    Task<AdminAction?> PreviousRepairAsync(Guid deliveryId, CancellationToken ct);
    void AddReplacement(GameEventOutboxMessage message);
}
