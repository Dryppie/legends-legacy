using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

public sealed class OperatorDraftRepository(LLDbContext db) : IOperatorDraftRepository
{
    public Task LockAsync(string actor, CancellationToken ct) => db.AcquireStateSyncScopeLockAsync($"operator-drafts:{actor}", ct);
    public Task<OperatorDraft?> GetAsync(string actor, string key, CancellationToken ct) =>
        db.Set<OperatorDraft>().SingleOrDefaultAsync(x => x.ActorSubject == actor && x.Key == key, ct);
    public void Add(OperatorDraft draft) => db.Set<OperatorDraft>().Add(draft);
}
