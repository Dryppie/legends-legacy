using Domain.Models.Administration;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

// A separate context commits the received reference before the action transaction starts.
// A rollback/crash of the action must not erase the administrator's recovery reference.
public sealed class OperatorOperationRepository(IDbContextFactory<LLDbContext> factory, TimeProvider time) : IOperatorOperationRepository
{
    public async Task<bool> BeginAsync(OperatorOperation operation, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.AcquireStateSyncScopeLockAsync($"operator-target:{operation.ActorSubject}:{operation.Environment}:{operation.Kind}:{operation.TargetId}", ct);
        await db.AcquireStateSyncScopeLockAsync($"operator-operation:{operation.ActorSubject}:{operation.Environment}:{operation.OperationId}", ct);
        var current = await db.Set<OperatorOperation>().SingleOrDefaultAsync(x => x.ActorSubject == operation.ActorSubject && x.Environment == operation.Environment && x.OperationId == operation.OperationId, ct);
        if (current is not null && (current.Kind != operation.Kind || current.Source != operation.Source || current.TargetId != operation.TargetId || current.TargetKind != operation.TargetKind)) return false;
        if (current is null && await db.Set<OperatorOperation>().AnyAsync(x => x.ActorSubject == operation.ActorSubject && x.Environment == operation.Environment &&
            x.Kind == operation.Kind && x.TargetId == operation.TargetId && x.Outcome == "Unknown" &&
            !(x.Source == "Game" && db.AdminActions.Any(a => a.Id == x.OperationId && a.ActorSubject == operation.ActorSubject)), ct)) return false;
        if (current is null) { current = operation; current.ReceivedAt = time.GetUtcNow(); db.Add(current); }
        current.Attempts++;
        if (current.Outcome != "Committed") current.Outcome = "Unknown";
        current.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return true;
    }
    public async Task FinishAsync(string actor, string environment, Guid id, string outcome, CancellationToken ct)
    {
        if (outcome is not ("Committed" or "Rejected")) return;
        await using var db = await factory.CreateDbContextAsync(ct);
        // A rejected retry can never prove that an earlier uncertain attempt did not commit.
        var query = db.Set<OperatorOperation>().Where(x => x.ActorSubject == actor && x.Environment == environment && x.OperationId == id && x.Outcome != "Committed");
        if (outcome == "Rejected") query = query.Where(x => x.Attempts == 1 && !db.AdminActions.Any(a => a.Id == id && a.ActorSubject == actor));
        await query.ExecuteUpdateAsync(set => set.SetProperty(x => x.Outcome, outcome).SetProperty(x => x.UpdatedAt, time.GetUtcNow()), ct);
    }
    public async Task<OperatorOperationPage> SearchAsync(string actor, string environment, int page, bool unresolvedOnly, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 10000);
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.Set<OperatorOperation>().AsNoTracking().Where(x => x.ActorSubject == actor && x.Environment == environment);
        if (unresolvedOnly) query = query.Where(x => x.Outcome == "Unknown" && !(x.Source == "Game" && db.AdminActions.Any(a => a.Id == x.OperationId && a.ActorSubject == actor)));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.OperationId).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var ids = rows.Where(x => x.Source == "Game").Select(x => x.OperationId).ToArray();
        var committed = await db.AdminActions.Where(x => ids.Contains(x.Id) && x.ActorSubject == actor).Select(x => x.Id).ToListAsync(ct);
        foreach (var row in rows) if (row.Source == "Game" && committed.Contains(row.OperationId)) row.Outcome = "Committed";
        return new(rows, total, page, 25);
    }
    public async Task<OperatorOperation?> GetAsync(string actor, string environment, Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.Set<OperatorOperation>().AsNoTracking().SingleOrDefaultAsync(x => x.ActorSubject == actor && x.Environment == environment && x.OperationId == id, ct);
        if (row?.Source == "Game" && await db.AdminActions.AnyAsync(x => x.Id == id && x.ActorSubject == actor, ct)) row.Outcome = "Committed";
        return row;
    }
    public async Task<OperationDeliverySummary> DeliveriesAsync(Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.GameEventOutboxDeliveries.AsNoTracking().Where(x => x.Message.AdministrationOperationId == id);
        var counts = await query.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        var rows = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Take(50).Select(x => new OperationDelivery(x.Id, x.Consumer, x.Status, x.Attempts, x.CreatedAt,
            db.AdminActions.Where(a => a.TargetResourceId == x.Id && a.ActionType == AdminActionType.StateRefreshDeliveryRetried)
                .Select(a => (Guid?)a.Id).FirstOrDefault())).ToListAsync(ct);
        return new(counts.GetValueOrDefault(GameEventOutboxDeliveryStatus.Pending), counts.GetValueOrDefault(GameEventOutboxDeliveryStatus.Processing), counts.GetValueOrDefault(GameEventOutboxDeliveryStatus.Processed), counts.GetValueOrDefault(GameEventOutboxDeliveryStatus.Failed), rows);
    }
}
