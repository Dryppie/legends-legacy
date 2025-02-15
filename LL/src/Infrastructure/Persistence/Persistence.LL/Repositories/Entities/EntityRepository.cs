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

    public EntityRepository(IDbContext context)
    {
        _context = context;
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
        // Query only distinct IDs
        var entities = await _context.Entities
            .Include(e => e.BaseAttributes)
            .Include(e => e.EquippedEssences)
            .Include(e => (e as Creature).LootTable)
                .ThenInclude(lt => lt.Entries)
                .ThenInclude(lt => (lt as LootTable).Entries)
                .ThenInclude(lte => (lte as LootTableItem).Item)
            .Where(e => entityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        // Step 2: Check for any missing IDs
        var foundIds = entities.Select(e => e.Id).ToHashSet();
        var missingIds = entityIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            // Handle however you wish:
            // throw new NotFoundException(...);
            NotFoundException.ThrowIfNull(missingIds, nameof(entities), entityIds);
        }

        // Step 3: Build a dictionary from Id to Entity
        var entityLookup = entities.ToDictionary(e => e.Id, e => e);

        // Step 4: Reconstruct final list, preserving duplicates
        var finalList = new List<Entity>(entityIds.Count);
        foreach (var id in entityIds)
        {
            // Assuming none of the IDs are missing by now
            finalList.Add(entityLookup[id]);
        }

        return finalList;
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