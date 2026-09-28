using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

public sealed class SupportCaseRepository(LLDbContext db) : ISupportCaseRepository
{
    public async Task LockAsync(Guid operationId, Guid caseId, CancellationToken ct)
    {
        await db.AcquireStateSyncScopeLockAsync($"support-operation:{operationId:D}", ct);
        await db.AcquireStateSyncScopeLockAsync($"support-case:{caseId:D}", ct);
    }
    public Task<SupportCase?> GetAsync(Guid id, CancellationToken ct) =>
        db.Set<SupportCase>().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<SupportCaseEntry?> GetOperationAsync(Guid id, CancellationToken ct) =>
        db.Set<SupportCaseEntry>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<SupportCasePage> SearchAsync(Guid? characterId, SupportCaseStatus? status, string? search, int page, CancellationToken ct)
    {
        var q = db.Set<SupportCase>().AsNoTracking().AsQueryable();
        if (characterId.HasValue) q = q.Where(x => x.CharacterId == characterId);
        if (status.HasValue) q = q.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            if (Guid.TryParse(term, out var id)) q = q.Where(x => x.Id == id || x.AccountId == id);
            else q = q.Where(x => x.Title.ToLower().Contains(term) || x.CharacterName.ToLower().Contains(term) ||
                (x.ExternalReference != null && x.ExternalReference.ToLower().Contains(term)));
        }
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        return new(rows, total, page, 25);
    }
    public async Task<IReadOnlyList<SupportCaseEntry>> EntriesAsync(Guid caseId, int? beforeSequence, CancellationToken ct) =>
        await db.Set<SupportCaseEntry>().AsNoTracking().Where(x => x.CaseId == caseId &&
            (!beforeSequence.HasValue || x.Sequence < beforeSequence.Value)).OrderByDescending(x => x.Sequence).Take(50).ToListAsync(ct);
    public void Add(SupportCase value) => db.Set<SupportCase>().Add(value);
    public void Append(SupportCaseEntry value) => db.Set<SupportCaseEntry>().Add(value);
}
