using Application.Common.Interfaces;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class PlainEquipmentRepository(IDbContext db) : IPlainEquipmentRepository
{
    public async Task<IReadOnlyList<PlainEquipmentEntitlement>> GetAsync(Guid id, CancellationToken ct) =>
        (await db.PlainEquipmentEntitlements.Where(x => x.CharacterId == id).ToListAsync(ct))
        .Concat(db.PlainEquipmentEntitlements.Local.Where(x => x.CharacterId == id)).DistinctBy(x => (x.DefinitionId, x.Tier)).ToArray();
    public async Task RecordAwardAsync(Guid id, EquipmentData award, CancellationToken ct)
    {
        var entitlement = (await GetAsync(id, ct)).SingleOrDefault(x => x.DefinitionId == award.State.DefinitionId && x.Tier == award.State.Tier);
        if (entitlement == null)
        {
            entitlement = new() { CharacterId = id, DefinitionId = award.State.DefinitionId, Tier = award.State.Tier };
            db.PlainEquipmentEntitlements.Add(entitlement);
        }
        entitlement.RecordAward(award);
    }
}
