using Application.Common.Interfaces;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentBlueprintRepository(IDbContext db) : IEquipmentBlueprintRepository
{
    public async Task<EquipmentBlueprintProgress> LoadForCompletionAsync(Guid characterId, string familyId, CancellationToken ct)
    {
        await db.AcquireCharacterRowsLockAsync([characterId], ct);
        var progress = db.EquipmentBlueprintProgress.Local.SingleOrDefault(x => x.CharacterId == characterId && x.FamilyId == familyId)
            ?? await db.EquipmentBlueprintProgress.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.FamilyId == familyId, ct);
        if (progress is not null) return progress;
        progress = new() { CharacterId = characterId, FamilyId = familyId };
        db.EquipmentBlueprintProgress.Add(progress);
        return progress;
    }

    public async Task<IReadOnlyList<EquipmentBlueprintProgress>> GetProgressAsync(Guid characterId, CancellationToken ct) =>
        await db.EquipmentBlueprintProgress.AsNoTracking().Where(x => x.CharacterId == characterId).ToListAsync(ct);
}
