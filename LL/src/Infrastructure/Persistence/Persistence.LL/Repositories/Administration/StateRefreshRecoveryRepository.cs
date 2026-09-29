using Domain.Models.Administration;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;
public sealed class StateRefreshRecoveryRepository(LLDbContext db) : IStateRefreshRecoveryRepository
{
    public async Task LockAsync(Guid operationId, Guid deliveryId, CancellationToken ct)
    {
        await db.AcquireStateSyncScopeLockAsync($"state-refresh-operation:{operationId}", ct);
        await db.AcquireStateSyncScopeLockAsync($"state-refresh-delivery:{deliveryId}", ct);
    }
    public Task<GameEventOutboxDelivery?> GetAsync(Guid deliveryId, CancellationToken ct) =>
        db.GameEventOutboxDeliveries.AsNoTracking().Include(x => x.Message).SingleOrDefaultAsync(x => x.Id == deliveryId, ct);
    public Task<AdminAction?> PreviousRepairAsync(Guid deliveryId, CancellationToken ct) =>
        db.AdminActions.AsNoTracking().Where(x => x.TargetResourceId == deliveryId && x.ActionType == AdminActionType.StateRefreshDeliveryRetried)
            .OrderByDescending(x => x.OccurredAt).FirstOrDefaultAsync(ct);
    public void AddReplacement(GameEventOutboxMessage message) => db.GameEventOutboxMessages.Add(message);
}
