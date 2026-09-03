using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace API.LiveOps.Support;

public sealed partial class LiveOpsPlayerSupportSnapshotService
{
    // Each section owns an isolated context, timeout and read-only bounded queries.
    private async Task<EquipmentSupportSnapshotDto> LoadEquipmentAsync(TargetRow target, CancellationToken ct)
    {
        const int limit = 100;
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var inventory = db.InventoryItems.AsNoTracking().Where(x => x.InventoryId == target.CharacterId);
        var slots = db.EquipmentSlots.AsNoTracking().Where(x => x.EntityId == target.CharacterId);
        var listings = db.MarketPlaceListings.AsNoTracking().Where(x => x.SellerId == target.CharacterId);
        var loans = db.GuildVaultItems.AsNoTracking().Where(x => x.BorrowedByCharacterId == target.CharacterId);
        var equipmentQuery = db.Set<EquipmentInstance>().AsNoTracking().Where(item =>
            inventory.Any(x => x.ItemInstanceId == item.Id) || slots.Any(x => x.EquipmentInstanceId == item.Id)
            || listings.Any(x => x.ItemInstanceId == item.Id) || loans.Any(x => x.EquipmentInstanceId == item.Id));
        var equipmentCount = await equipmentQuery.CountAsync(ct);
        var items = await equipmentQuery.Include(x => x.ItemBase).OrderBy(x => x.Id).Take(limit).ToListAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var inventoryIds = await inventory.Where(x => ids.Contains(x.ItemInstanceId)).Select(x => x.ItemInstanceId).ToListAsync(ct);
        var equipped = await slots.Where(x => x.EquipmentInstanceId.HasValue && ids.Contains(x.EquipmentInstanceId.Value))
            .Select(x => new { x.EquipmentInstanceId, x.EquipmentSlotType }).ToListAsync(ct);
        var listedIds = await listings.Where(x => ids.Contains(x.ItemInstanceId)).Select(x => x.ItemInstanceId).ToListAsync(ct);
        var loanIds = await loans.Where(x => ids.Contains(x.EquipmentInstanceId)).Select(x => x.EquipmentInstanceId).ToListAsync(ct);
        var mapped = items.Select(item =>
        {
            var locations = new List<string>();
            if (inventoryIds.Contains(item.Id)) locations.Add("Inventory");
            locations.AddRange(equipped.Where(x => x.EquipmentInstanceId == item.Id).Select(x => $"Equipped: {x.EquipmentSlotType}"));
            if (listedIds.Contains(item.Id)) locations.Add("Marketplace escrow");
            if (loanIds.Contains(item.Id)) locations.Add("Guild loan");
            return new EquipmentSupportItemDto(item.Id, item.ItemBaseId, item.DisplayName, locations,
                DescribeEquipment(item.ProgressionData));
        }).ToArray();

        var pendingQuery = db.EquipmentProtectionReceipts.AsNoTracking()
            .Where(x => x.CharacterId == target.CharacterId && x.ClaimedAtUtc == null);
        var pendingCount = await pendingQuery.CountAsync(ct);
        var pending = await pendingQuery.OrderBy(x => x.RunId).Take(limit).ToListAsync(ct);
        var protection = await db.EquipmentProtectionProgress.AsNoTracking()
            .Where(x => x.CharacterId == target.CharacterId).OrderBy(x => x.PoolId).Take(limit + 1).ToListAsync(ct);
        var ordinary = await db.CombatAcquisitionProgress.AsNoTracking()
            .Where(x => x.CharacterId == target.CharacterId).OrderBy(x => x.PoolId).Take(limit + 1).ToListAsync(ct);
        var dungeonRun = await LoadEquipmentDungeonRunAsync(db, target.CharacterId, limit, ct);

        return new(limit, equipmentCount, pendingCount,
            protection.Count > limit || ordinary.Count > limit,
            mapped,
            pending.Select(x => new EquipmentSupportPendingRewardDto(x.RunId, x.Outcome.PoolId, x.Outcome.SecuredAtUtc,
                x.Outcome.Equipment is { } data
                    ? new(data.State.Id, data.ItemBaseId, data.DisplayName, ["Pending dungeon reward"], DescribeEquipment(data)) : null)).ToArray(),
            protection.Take(limit).Select(x => new EquipmentSupportProtectionDto(x.PoolId, x.SelectedDefinitionId,
                x.CompletionsWithoutMatch, x.Revision)).ToArray(),
            ordinary.Take(limit).Select(x => new EquipmentSupportOrdinaryDto(x.PoolId, x.HasEnteredRegion,
                x.Plain?.Equipment.State.DefinitionId, x.PlainVictories, x.Plain?.RequiredVictories,
                x.Sigil?.FamilyId, x.SigilVictories, x.Sigil?.RequiredVictories, x.Revision,
                x.LastEncounterAtUtc)).ToArray())
        { DungeonRun = dungeonRun };
    }

    private static EquipmentSupportDescriptorDto? DescribeEquipment(EquipmentData? data)
    {
        if (data is null) return null;
        var state = data.State;
        return new(state.DefinitionId, state.ArchetypeId, state.Tier, state.Rank, state.BalanceVersion,
            data.Rarity.ToString(), state.NativeStyleId, state.ActiveStyleId, state.Ownership.Kind.ToString(),
            state.Ownership.OwnerId, state.Provenance.Kind.ToString(), state.Provenance.SourceId,
            state.Provenance.AwardId);
    }
}
