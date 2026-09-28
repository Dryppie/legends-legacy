using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentMigrationRepository(LLDbContext db) : IEquipmentMigrationRepository
{
    public async Task AssertNoScheduledCombatAsync(IReadOnlyList<Guid> characterIds, CancellationToken ct)
    {
        await db.AcquireCharacterRowsLockAsync(characterIds, ct);
        if (await db.CharacterActions.AnyAsync(x => characterIds.Contains(x.CharacterId) && !x.IsDeleted && x.NextResolutionAtUtc != null, ct))
            throw new InvalidOperationException("Stop and settle scheduled combat before operator migration.");
    }

    public async Task<EquipmentMigrationAudit> AuditAsync(int page, int pageSize, CancellationToken ct, int sourceBalanceVersion = 1)
    {
        if (sourceBalanceVersion < 1) throw new ArgumentOutOfRangeException(nameof(sourceBalanceVersion));
        page = Math.Max(0, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await db.ItemInstances.OfType<EquipmentInstance>().CountAsync(ct);
        var missing = await db.ItemInstances.OfType<EquipmentInstance>().CountAsync(x => x.ProgressionData == null, ct);
        var legacy = await db.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"ItemInstances\" WHERE (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int = {sourceBalanceVersion}").SingleAsync(ct);
        var pending = await db.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RunRewards\" WHERE EXISTS (SELECT 1 FROM \"DungeonRuns\" WHERE \"DungeonRuns\".\"Id\" = \"RunRewards\".\"DungeonRunId\" AND \"RewardsClaimedAt\" IS NULL) AND (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int = {sourceBalanceVersion}").SingleAsync(ct);
        var targets = await db.Database.SqlQuery<AuditTarget>($"SELECT \"Id\" AS \"ItemId\", 0 AS \"Location\", NULL::uuid AS \"ContainerId\" FROM \"ItemInstances\" WHERE (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int = {sourceBalanceVersion} OR ({sourceBalanceVersion} = 1 AND \"ItemType\" = 0 AND \"ModelEData\" IS NULL) UNION ALL SELECT (\"ModelEData\" #>> '{{State,Id}}')::uuid AS \"ItemId\", 1 AS \"Location\", \"DungeonRunId\" AS \"ContainerId\" FROM \"RunRewards\" WHERE EXISTS (SELECT 1 FROM \"DungeonRuns\" WHERE \"DungeonRuns\".\"Id\" = \"RunRewards\".\"DungeonRunId\" AND \"RewardsClaimedAt\" IS NULL) AND (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int = {sourceBalanceVersion} ORDER BY \"ItemId\" LIMIT {pageSize} OFFSET {checked(page * pageSize)}").ToListAsync(ct);
        return new(page, pageSize, total, legacy, missing, pending, await db.CharacterSnapshots.CountAsync(ct),
            targets.Select(x => new EquipmentMigrationTarget(x.ItemId, (EquipmentMigrationLocation)x.Location, x.ContainerId)).ToArray())
        {
            SourceBalanceVersion = sourceBalanceVersion,
            UnreferencedUnversionedInstances = await db.ItemInstances.OfType<EquipmentInstance>().CountAsync(x => x.ProgressionData == null
                && !db.InventoryItems.Any(i => i.ItemInstanceId == x.Id)
                && !db.EquipmentSlots.Any(s => s.EquipmentInstanceId == x.Id)
                && !db.MarketPlaceListings.Any(m => m.ItemInstanceId == x.Id)
                && !db.GuildVaultItems.Any(g => g.EquipmentInstanceId == x.Id)
                && !db.EquipmentLoadoutSlots.Any(l => l.EquipmentInstanceId == x.Id), ct),
            UnversionedPendingRewards = await db.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RunRewards\" WHERE \"ItemType\" = 0 AND \"ModelEData\" IS NULL AND EXISTS (SELECT 1 FROM \"DungeonRuns\" WHERE \"DungeonRuns\".\"Id\" = \"RunRewards\".\"DungeonRunId\" AND \"RewardsClaimedAt\" IS NULL)").SingleAsync(ct),
            LegacyArenaDefenses = await db.ArenaDefenseSnapshots.CountAsync(x => x.IsValid && x.CharacterSnapshot.AttributeRulesVersion == 17, ct),
            ActiveLegacyTournamentSnapshots = await db.TournamentCombatSnapshots.CountAsync(x =>
                x.Tournament.Status != Domain.Models.Colosseum.Tournaments.TournamentStatus.Completed
                && x.Tournament.Status != Domain.Models.Colosseum.Tournaments.TournamentStatus.Cancelled
                && x.CharacterSnapshot.AttributeRulesVersion == 17, ct)
        };
    }

    public async Task<IReadOnlyList<Guid>> GetAffectedCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct)
    {
        if (target.Location == EquipmentMigrationLocation.PendingDungeonReward)
            return await db.DungeonRuns.Where(x => x.Id == target.ContainerId).Select(x => x.CharacterId).ToArrayAsync(ct);
        return await db.EquipmentSlots.Where(x => x.EquipmentInstanceId == target.ItemId).Select(x => x.EntityId)
            .Union(db.InventoryItems.Where(x => x.ItemInstanceId == target.ItemId).Select(x => x.InventoryId))
            .Union(db.MarketPlaceListings.Where(x => x.ItemInstanceId == target.ItemId).Select(x => x.SellerId))
            .Union(db.GuildVaultItems.Where(x => x.EquipmentInstanceId == target.ItemId && x.BorrowedByCharacterId != null)
                .Select(x => x.BorrowedByCharacterId!.Value))
            .Union(db.EquipmentLoadoutSlots.Where(x => x.EquipmentInstanceId == target.ItemId)
                .Join(db.EquipmentLoadouts, slot => slot.EquipmentLoadoutId, loadout => loadout.Id, (slot, loadout) => loadout.CharacterId))
            .Where(id => db.Characters.Any(character => character.Id == id)).Distinct().OrderBy(id => id).ToArrayAsync(ct);
    }

    public async Task<EquipmentData?> LoadAsync(EquipmentMigrationTarget target, bool forMutation, CancellationToken ct)
    {
        if (forMutation)
        {
            if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Migration requires the command transaction.");
            await db.AcquireCharacterRowsLockAsync(await GetAffectedCharactersAsync(target, ct), ct);
            if (target.Location == EquipmentMigrationLocation.Instance)
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"ItemInstances\" WHERE \"Id\" = {target.ItemId} FOR UPDATE", ct);
            else
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"DungeonRuns\" WHERE \"Id\" = {target.ContainerId} FOR UPDATE", ct);
        }
        if (target.Location == EquipmentMigrationLocation.PendingDungeonReward)
        {
            var run = await db.DungeonRuns.Include(x => x.PendingRewards).SingleOrDefaultAsync(x => x.Id == target.ContainerId, ct);
            return run?.RewardsClaimedAt is null ? run?.PendingRewards.SingleOrDefault(x => x.ProgressionData?.State.Id == target.ItemId)?.ProgressionData : null;
        }
        var query = db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.ItemBase).Include(x => x.InstanceModifiers);
        var item = await query.SingleOrDefaultAsync(x => x.Id == target.ItemId, ct);
        if (forMutation && item is not null) await db.Entry(item).ReloadAsync(ct);
        return item?.ProgressionData;
    }

    public async Task<LegacyEquipmentSnapshot?> LoadLegacyAsync(EquipmentMigrationTarget target, CancellationToken ct)
    {
        if (target.Location != EquipmentMigrationLocation.Instance) return null;
        var item = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.InstanceModifiers)
            .SingleOrDefaultAsync(x => x.Id == target.ItemId, ct);
        if (item is null || item.ProgressionData is not null) return null;
        return await SnapshotLegacyAsync(item, ct);
    }

    private async Task<LegacyEquipmentSnapshot> SnapshotLegacyAsync(EquipmentInstance item, CancellationToken ct)
    {
        var itemBase = await db.ItemBases.OfType<EquipmentBase>().Include(x => x.AttributeModifiers)
            .SingleAsync(x => x.Id == item.ItemBaseId, ct);
        var inventory = await db.InventoryItems.Where(x => x.ItemInstanceId == item.Id)
            .Select(x => new { Owner = x.InventoryId, x.Quantity }).ToArrayAsync(ct);
        var slots = await db.EquipmentSlots.Where(x => x.EquipmentInstanceId == item.Id).Select(x => x.EntityId).ToArrayAsync(ct);
        var listings = await db.MarketPlaceListings.Where(x => x.ItemInstanceId == item.Id)
            .Select(x => new { Owner = x.SellerId, x.Quantity }).ToArrayAsync(ct);
        var guild = await db.GuildVaultItems.SingleOrDefaultAsync(x => x.EquipmentInstanceId == item.Id, ct);
        var owners = inventory.Select(x => x.Owner).Concat(slots).Concat(listings.Select(x => x.Owner)).Distinct().ToArray();
        if (inventory.Any(x => x.Quantity != 1) || listings.Any(x => x.Quantity != 1)
            || inventory.Length + listings.Length > 1 || (inventory.Length + listings.Length > 0 && slots.Length > 0))
            throw new InvalidOperationException("Legacy equipment has conflicting locations or quantities; reconcile ownership first.");
        EquipmentOwnership ownership;
        if (guild is not null)
        {
            if (listings.Length != 0 || owners.Any(x => x != guild.BorrowedByCharacterId))
                throw new InvalidOperationException("Legacy guild equipment has inconsistent loan ownership.");
            ownership = new(EquipmentOwnershipKind.GuildOwned, guild.GuildId);
        }
        else
        {
            if (owners.Length != 1 || !await db.Characters.AnyAsync(x => x.Id == owners[0], ct))
                throw new InvalidOperationException("Legacy equipment has no unique current character or guild owner. Unreferenced records are retained without conversion.");
            ownership = new(itemBase.IsBound ? EquipmentOwnershipKind.BoundPersonal : EquipmentOwnershipKind.UnboundPersonal, owners[0]);
        }
        return new(item.Id, item.ItemBaseId, itemBase.Name, itemBase.EquipmentType, item.Rarity, item.Quality, item.Tier,
            itemBase.IsBound, ownership, item.AcquiredAtUtc, item.AcquisitionSource, item.IsFavorite, item.AffinityTags.ToArray(),
            itemBase.AttributeModifiers.Select(x => new LegacyEquipmentModifier(x.Id, x.AttributeType, x.Amount, x.ModifierType)).OrderBy(x => x.Id).ToArray(),
            item.InstanceModifiers.Select(x => new LegacyEquipmentModifier(x.Id, x.AttributeType, x.Amount, x.ModifierType, x.RarityBonusAmount)).OrderBy(x => x.Id).ToArray());
    }

    public async Task RestoreLegacyAsync(EquipmentMigrationTarget target, LegacyEquipmentSnapshot original,
        EquipmentMigrationReceipt receipt, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || target.Location != EquipmentMigrationLocation.Instance)
            throw new InvalidOperationException("Legacy restoration requires an instance and the command transaction.");
        var item = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.InstanceModifiers)
            .SingleAsync(x => x.Id == target.ItemId, ct);
        var current = await SnapshotLegacyAsync(item, ct);
        if ((current with { InstanceModifiers = original.InstanceModifiers }).Hash() != original.Hash())
            throw new InvalidOperationException("Legacy equipment metadata, base modifiers or ownership changed. Rollback requires manual reconciliation.");
        db.RemoveRange(item.InstanceModifiers);
        item.RestoreLegacyMigration(original);
        // Restored IDs are intentionally nonempty; explicitly insert them instead of
        // letting EF infer that these previously deleted rows should be updated.
        db.AddRange(item.InstanceModifiers);
        if (receipt.RolledBackAtUtc is { } rollback) receipt.RolledBackAtUtc = StoredTimestamp(rollback);
    }

    public async Task<EquipmentMigrationReceipt?> GetReceiptAsync(Guid operationId, CancellationToken ct)
    {
        var receipt = await db.Set<EquipmentMigrationReceipt>().SingleOrDefaultAsync(x => x.OperationId == operationId, ct);
        if (receipt is not null) await db.Entry(receipt).ReloadAsync(ct);
        return receipt;
    }

    public Task<EquipmentMigrationReceipt?> GetActiveReceiptForItemAsync(Guid itemId, CancellationToken ct) =>
        db.Set<EquipmentMigrationReceipt>().Where(x => x.ItemId == itemId && x.RolledBackAtUtc == null)
            .OrderByDescending(x => x.Revision).FirstOrDefaultAsync(ct);

    public async Task SaveAsync(EquipmentMigrationTarget target, EquipmentData data, EquipmentMigrationReceipt receipt, bool isNew, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Migration requires the command transaction.");
        // PostgreSQL stores microseconds. Return the same receipt timestamps on the
        // initial response and retries, rather than leaking unpersistable .NET ticks.
        receipt.AppliedAtUtc = StoredTimestamp(receipt.AppliedAtUtc);
        if (receipt.RolledBackAtUtc is { } rollback) receipt.RolledBackAtUtc = StoredTimestamp(rollback);
        if (receipt.ChoiceUsedAtUtc is { } choice) receipt.ChoiceUsedAtUtc = StoredTimestamp(choice);
        if (target.Location == EquipmentMigrationLocation.Instance)
        {
            var item = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.InstanceModifiers).SingleAsync(x => x.Id == target.ItemId, ct);
            db.RemoveRange(item.InstanceModifiers);
            item.ApplyProgressionData(data);
        }
        else
        {
            var run = await db.DungeonRuns.Include(x => x.PendingRewards).SingleAsync(x => x.Id == target.ContainerId, ct);
            var reward = run.PendingRewards.Single(x => x.ProgressionData?.State.Id == target.ItemId);
            reward.ProgressionData = data;
            run.RowVersion++;
        }
        if (isNew) db.Set<EquipmentMigrationReceipt>().Add(receipt);
    }

    private static DateTimeOffset StoredTimestamp(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % TimeSpan.TicksPerMicrosecond, TimeSpan.Zero);

    private sealed class AuditTarget
    {
        public Guid ItemId { get; set; }
        public int Location { get; set; }
        public Guid? ContainerId { get; set; }
    }
}
