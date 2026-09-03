using Application.Common.Interfaces;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class StarterEquipmentRepository(IDbContext db) : IStarterEquipmentRepository
{
    public async Task<StarterEquipmentGrant?> GetGrantAsync(Guid characterId, StarterEquipmentGrantKind kind, CancellationToken cancellationToken) =>
        db.StarterEquipmentGrants.Local.SingleOrDefault(x => x.CharacterId == characterId && x.Kind == kind)
        ?? await db.StarterEquipmentGrants.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.Kind == kind, cancellationToken);

    public Task<bool> HasInventoryAsync(Guid characterId, CancellationToken cancellationToken) =>
        db.Inventories.AnyAsync(x => x.CharacterId == characterId, cancellationToken);

    public void AddGrant(StarterEquipmentGrant grant) => db.StarterEquipmentGrants.Add(grant);
}
