using Application.Common.Interfaces;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;
public sealed class CombatAcquisitionRepository(IDbContext db) : ICombatAcquisitionRepository
{
    public Task LockAsync(Guid id, CancellationToken ct) => db.AcquireCharacterRowsLockAsync([id], ct);
    public Task<int?> GetLevelAsync(Guid id, CancellationToken ct) => db.Characters.Where(x => x.Id == id).Select(x => (int?)x.Level).SingleOrDefaultAsync(ct);
    public Task<bool> HasClearedTowerFloorAsync(string serverId, int floor, CancellationToken ct) =>
        db.TowerFloorProgresses.AsNoTracking().AnyAsync(p => p.ServerId == serverId && p.IsCleared && p.FloorNumber >= floor, ct);
    public async Task<CombatAcquisitionProgress?> GetAsync(Guid id, string pool, CancellationToken ct) =>
        db.CombatAcquisitionProgress.Local.SingleOrDefault(x => x.CharacterId == id && x.PoolId == pool)
        ?? await db.CombatAcquisitionProgress.SingleOrDefaultAsync(x => x.CharacterId == id && x.PoolId == pool, ct);
    public void Add(CombatAcquisitionProgress progress) => db.CombatAcquisitionProgress.Add(progress);
    public async Task<CombatAcquisitionSelectionReceipt?> GetSelectionAsync(Guid id, Guid operation, CancellationToken ct) =>
        db.CombatAcquisitionSelectionReceipts.Local.SingleOrDefault(x => x.CharacterId == id && x.OperationId == operation)
        ?? await db.CombatAcquisitionSelectionReceipts.SingleOrDefaultAsync(x => x.CharacterId == id && x.OperationId == operation, ct);
    public void AddSelection(CombatAcquisitionSelectionReceipt receipt) => db.CombatAcquisitionSelectionReceipts.Add(receipt);
}
