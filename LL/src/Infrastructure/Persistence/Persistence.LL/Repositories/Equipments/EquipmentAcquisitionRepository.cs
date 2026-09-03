using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentAcquisitionRepository(IDbContext db) : IEquipmentAcquisitionRepository
{
    public Task<int?> GetLevelAsync(Guid id, CancellationToken ct) => db.Characters.Where(x => x.Id == id).Select(x => (int?)x.Level).SingleOrDefaultAsync(ct);
    public Task LockAsync(Guid id, CancellationToken ct) => db.AcquireCharacterRowsLockAsync([id], ct);
    public async Task<EquipmentProtectionProgress?> GetProgressAsync(Guid id, string pool, CancellationToken ct) =>
        db.EquipmentProtectionProgress.Local.SingleOrDefault(x => x.CharacterId == id && x.PoolId == pool)
        ?? await db.EquipmentProtectionProgress.SingleOrDefaultAsync(x => x.CharacterId == id && x.PoolId == pool, ct);
    public void AddProgress(EquipmentProtectionProgress progress) => db.EquipmentProtectionProgress.Add(progress);
    public async Task<EquipmentProtectionReceipt?> GetCompletionAsync(Guid id, Guid run, CancellationToken ct) =>
        db.EquipmentProtectionReceipts.Local.SingleOrDefault(x => x.CharacterId == id && x.RunId == run)
        ?? await db.EquipmentProtectionReceipts.SingleOrDefaultAsync(x => x.CharacterId == id && x.RunId == run, ct);
    public void AddCompletion(EquipmentProtectionReceipt receipt) => db.EquipmentProtectionReceipts.Add(receipt);
    public async Task<BaselineEquipmentRecoveryReceipt?> GetRecoveryAsync(Guid id, Guid operation, CancellationToken ct) =>
        db.BaselineEquipmentRecoveryReceipts.Local.SingleOrDefault(x => x.CharacterId == id && x.OperationId == operation)
        ?? await db.BaselineEquipmentRecoveryReceipts.SingleOrDefaultAsync(x => x.CharacterId == id && x.OperationId == operation, ct);
    public void AddRecovery(BaselineEquipmentRecoveryReceipt receipt) => db.BaselineEquipmentRecoveryReceipts.Add(receipt);

    public async Task<IReadOnlyList<EquipmentData>> GetOwnedAndPendingAsync(Guid id, CancellationToken ct)
    {
        var inventory = await db.InventoryItems.Include(x => x.ItemInstance).Where(x => x.InventoryId == id).ToListAsync(ct);
        var slots = await db.EquipmentSlots.Include(x => x.EquipmentInstance).Where(x => x.EntityId == id).ToListAsync(ct);
        var runs = await db.DungeonRuns.Include(x => x.PendingRewards).Where(x => x.CharacterId == id).ToListAsync(ct);
        var pending = await db.EquipmentProtectionReceipts.Where(x => x.CharacterId == id && x.ClaimedAtUtc == null).ToListAsync(ct);
        return inventory.Concat(db.InventoryItems.Local.Where(x => x.InventoryId == id))
            .Where(x => x.Quantity > 0 && db.GetEntry(x).State != EntityState.Deleted).Select(x => (x.ItemInstance as EquipmentInstance)?.ProgressionData)
            .Concat(slots.Select(x => x.EquipmentInstance?.ProgressionData))
            .Concat(runs.SelectMany(x => x.PendingRewards).Select(x => x.ProgressionData))
            .Concat(pending.Select(x => x.Outcome.Equipment))
            .Where(x => x != null).Cast<EquipmentData>().DistinctBy(x => x.State.Id).ToArray();
    }

    public Task<IReadOnlyList<InventoryItem>> AwardRecoveryAsync(Guid id, BaselineEquipmentRecovery recovery, CancellationToken ct) =>
        AwardRecoveredItemsAsync(db, id, recovery.OperationId, recovery.Equipment, recovery.RecoveredAtUtc, EquipmentKeys.BaselineRecoverySource, ct);

    internal static async Task<IReadOnlyList<InventoryItem>> AwardRecoveredItemsAsync(IDbContext db, Guid id, Guid operationId,
        IReadOnlyList<EquipmentData> equipment, DateTimeOffset now, string source, CancellationToken ct)
    {
        var character = await db.Characters.SingleAsync(x => x.Id == id, ct);
        if (!await db.Inventories.AnyAsync(x => x.CharacterId == id, ct)) throw new InvalidOperationException("Character inventory was not found.");
        var ids = equipment.Select(x => x.ItemBaseId).Distinct().ToArray();
        var bases = await db.ItemBases.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (equipment.Any(x => !bases.TryGetValue(x.ItemBaseId, out var itemBase)
            || itemBase is not EquipmentBase equipmentBase || equipmentBase.EquipmentType != x.EquipmentType || itemBase.Stackable))
            throw new InvalidOperationException("Recovery equipment bases are unavailable.");
        var items = equipment.Select(data =>
        {
            var instance = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId, ItemBase = bases[data.ItemBaseId],
                AcquiredAtUtc = now, AcquisitionSource = source };
            instance.ApplyProgressionData(data);
            return new InventoryItem { InventoryId = id, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 1 };
        }).ToArray();
        db.InventoryItems.AddRange(items);
        foreach (var item in items) db.EconomyLedger.Add(new()
        {
            EventType = EconomyEventType.ItemAcquisition, AssetType = EconomyAssetType.Item, ReferenceId = operationId,
            RecipientCharacterId = id, RecipientAccountId = character.UserId, RecipientCharacterLevel = character.Level,
            AssetId = item.ItemInstance.ItemBaseId, AssetName = item.ItemInstance.ItemBase.Name, Quantity = 1,
            DestinationItemInstanceId = item.ItemInstanceId, Source = source, OccurredAt = now
        });
        return items;
    }
}
