using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities;

public class EntityRepository : IEntityRepository
{
    private readonly IDbContext _context;

    public EntityRepository(IDbContext context)
    {
        _context = context;
    }

    public void UpdateEntities(List<Entity> entities)
    {
        if (entities.Count == 0) return;
        _context.Entities.UpdateRange(entities);
    }

    public async Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken)
    {
        var entities = await _context.Entities
            .Include(e => e.BaseAttributes)
            .Include(e => e.EquipmentSlots)
                .ThenInclude(es => (es.EquipmentInstance.ItemBase as EquipmentBase).AttributeModifiers)
            .Include(e => e.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance.ToolAffixes)
            .Include(e => e.EquipmentSlots)
                .ThenInclude(es => (es.EquipmentInstance.ItemBase as EquipmentBase).ToolBonuses)
            .Include(e => (e as Creature)!.StatOverrides)
            .Where(e => entityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var foundIds = entities.Select(e => e.Id).ToHashSet();
        var missingIds = entityIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            NotFoundException.ThrowIfNull(missingIds, nameof(entities), entityIds);
        }

        var entityLookup = entities.ToDictionary(e => e.Id, e => e);
        return entityIds.Select(id => entityLookup[id]).ToList();
    }

    public async Task<Entity> GetEntityByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var entity = await _context.Entities
            .Include(e => e.BaseAttributes)
            .FirstOrDefaultAsync(e => e.Id.Equals(entityId), cancellationToken);

        NotFoundException.ThrowIfNull(entity, nameof(entity), entityId);
        return entity;
    }
}
