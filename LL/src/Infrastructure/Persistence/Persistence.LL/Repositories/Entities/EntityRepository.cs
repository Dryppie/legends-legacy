using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Entities;
public class EntityRepository : IEntityRepository
{
    private readonly IDbContext _context;

    public EntityRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    public async Task UpdateEntities(List<Entity> entities, CancellationToken cancellationToken)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        _context.Entities.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken)
    {
        var entities = await _context.Entities
            .Include(e => e.BaseAttributes)
            .Include(e => e.EquippedEssences)
            .Include(e => (e as Creature).LootTable)
                .ThenInclude(lt => lt.Entries)
                .ThenInclude(lt => (lt as LootTable).Entries) // Make sure to include the child LootTable to each creature's LootTable (Rarity tables)
                .ThenInclude(lte => (lte as LootTableItem).Item) // Make sure to include the items to each LootTableItem
            .Where(e => entityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var missingIds = entityIds.Except(entities.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            NotFoundException.ThrowIfNull(missingIds, nameof(entities), entityIds);
        }

        return entities;
    }

    public async Task<Entity> GetEntityByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var entity = await _context.Entities
            .Include(e => e.BaseAttributes)
            .Include(e => e.EquippedEssences)
            .FirstOrDefaultAsync(e => e.Id.Equals(entityId), cancellationToken);
        
        NotFoundException.ThrowIfNull(entity, nameof(entity), entityId);

        return entity;
    }

}