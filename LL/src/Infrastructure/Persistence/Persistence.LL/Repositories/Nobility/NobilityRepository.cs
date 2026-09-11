using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Nobility;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Nobility;

public sealed class NobilityRepository(LLDbContext db) : INobilityRepository
{
    public Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken ct) =>
        db.Characters.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == characterId, ct);

    public async Task LockAccountAsync(Guid accountId, Guid characterId, CancellationToken ct)
    {
        await db.AcquireCharacterRowsLockAsync([characterId], ct);
        await db.AcquireStateSyncScopeLockAsync($"nobility:account:{accountId:D}", ct);
    }

    public Task<NobilityMembership?> GetMembershipAsync(Guid accountId, CancellationToken ct) =>
        db.Set<NobilityMembership>().Include(x => x.Coverage).SingleOrDefaultAsync(x => x.AccountId == accountId, ct);

    public async Task<IReadOnlyList<NobilityCoverage>> GetCoverageAsync(Guid characterId, CancellationToken ct) =>
        await db.Set<NobilityCoverage>().AsNoTracking()
            .Where(x => db.Characters.Any(c => c.Id == characterId && c.UserId == x.AccountId))
            .OrderBy(x => x.StartsAt).ToListAsync(ct);

    public async Task<IReadOnlyList<SignetUnit>> GetUnitsAsync(Guid characterId, SignetState state, CancellationToken ct)
    {
        var persisted = await db.Set<SignetUnit>().Where(x => x.OwnerCharacterId == characterId && x.State == state).ToListAsync(ct);
        return persisted.Concat(db.Set<SignetUnit>().Local).DistinctBy(x => x.Id)
            .Where(x => x.OwnerCharacterId == characterId && x.State == state && db.Entry(x).State != EntityState.Deleted)
            .OrderBy(x => x.IssuedAt).ThenBy(x => x.IssuanceId).ThenBy(x => x.Ordinal).ToArray();
    }

    public Task<SignetIssuance?> GetIssuanceAsync(Guid operationId, CancellationToken ct) =>
        db.Set<SignetIssuance>().SingleOrDefaultAsync(x => x.Id == operationId, ct);
    public Task<SignetRedemption?> GetRedemptionAsync(Guid operationId, CancellationToken ct) =>
        db.Set<SignetRedemption>().SingleOrDefaultAsync(x => x.Id == operationId, ct);
    public Task<bool> HasCashSupportAsync(Guid accountId, CancellationToken ct) =>
        db.Set<SignetIssuance>().AnyAsync(x => x.AccountId == accountId && x.Origin == SignetOrigin.Purchase && !x.Refunded, ct);
    public void AddMembership(NobilityMembership membership) => db.Set<NobilityMembership>().Add(membership);
    public void AddIssuance(SignetIssuance issuance, IReadOnlyCollection<SignetUnit> units)
    {
        db.Set<SignetIssuance>().Add(issuance);
        db.Set<SignetUnit>().AddRange(units);
        if (issuance.Origin == SignetOrigin.AlphaGrant)
            db.AdminActions.Add(new Domain.Models.Administration.AdminAction
            {
                Id = issuance.Id, ActionType = Domain.Models.Administration.AdminActionType.AlphaSignetsGranted,
                Permission = Application.UseCases.Administration.AdministrationPermissions.EconomyCompensation, ActorSubject = issuance.ActorSubject!, ActorDisplayName = issuance.ActorSubject!,
                TargetAccountId = issuance.AccountId, TargetCharacterId = issuance.CharacterId,
                Reason = issuance.Reason, OccurredAt = issuance.IssuedAt,
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { issuance.Quantity, itemBaseId = NobilityBenefits.SignetItemId })
            });
    }
    public void AddMovement(SignetMovement movement) => db.Set<SignetMovement>().Add(movement);
    public void AddRedemption(SignetRedemption redemption) => db.Set<SignetRedemption>().Add(redemption);

    public async Task SynchronizeInventoryAsync(Guid characterId, CancellationToken ct)
    {
        var count = (await GetUnitsAsync(characterId, SignetState.Available, ct)).Count;
        var rows = await db.InventoryItems.Include(x => x.ItemInstance).ThenInclude(x => x.ItemBase)
            .Where(x => x.InventoryId == characterId && x.ItemInstance.ItemBaseId == NobilityBenefits.SignetItemId).ToListAsync(ct);
        rows = rows.Concat(db.InventoryItems.Local.Where(x => x.InventoryId == characterId &&
            x.ItemInstance?.ItemBaseId == NobilityBenefits.SignetItemId)).DistinctBy(x => x.ItemInstanceId)
            .Where(x => db.Entry(x).State != EntityState.Deleted).ToList();
        if (count == 0)
        {
            db.InventoryItems.RemoveRange(rows);
            return;
        }
        var row = rows.FirstOrDefault();
        if (row is null)
        {
            var itemBase = await db.ItemBases.SingleOrDefaultAsync(x => x.Id == NobilityBenefits.SignetItemId, ct)
                ?? db.ItemBases.Local.FirstOrDefault(x => x.Id == NobilityBenefits.SignetItemId);
            if (itemBase is null) throw new InvalidOperationException("The Signet catalog item is missing.");
            var instance = new ItemInstance
            {
                Id = Guid.NewGuid(), ItemBaseId = itemBase.Id, ItemBase = itemBase,
                AcquisitionSource = "Nobility", AcquiredAtUtc = DateTimeOffset.UtcNow
            };
            row = new InventoryItem { InventoryId = characterId, ItemInstance = instance, ItemInstanceId = instance.Id };
            db.InventoryItems.Add(row);
        }
        row.Quantity = count;
        db.InventoryItems.RemoveRange(rows.Skip(1));
    }
}
