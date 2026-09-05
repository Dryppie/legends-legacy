using Application.Common.Interfaces;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Items;

public class ItemBaseRepository : IItemBaseRepository
{
    private readonly IDbContext _context;

    public ItemBaseRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemBase>> GetTradableItemBasesAsync(CancellationToken cancellationToken) =>
        await _context.ItemBases
            .AsNoTracking()
            .Include(x => (x as EquipmentBase)!.AttributeModifiers)
            .AsSplitQuery()
            .Where(x => !x.IsBound)
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<string, ItemBase>();
        }

        return await _context.ItemBases
            .Include(x => (x as EquipmentBase)!.AttributeModifiers)
            .AsSplitQuery()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken)
    {
        var essenceItems = await _context.ItemBases
            .OfType<EssenceItemBase>()
            .Select(item => new { item.Id, item.EssenceDefinitionId })
            .ToListAsync(cancellationToken);

        var resolvedMappings = essenceItems
            .Select(item => new
            {
                item.Id,
                DefinitionId = EssenceItemBase.ResolveDefinitionId(item.Id, item.EssenceDefinitionId)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.DefinitionId))
            .ToList();

        var duplicateMapping = resolvedMappings
            .GroupBy(item => item.DefinitionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Skip(1).Any());

        if (duplicateMapping is not null)
        {
            var itemIds = string.Join(", ", duplicateMapping.Select(item => item.Id).Order(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(
                $"Multiple Essence item bases resolve to '{duplicateMapping.Key}': {itemIds}.");
        }

        return resolvedMappings.ToDictionary(
            item => item.DefinitionId,
            item => item.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken)
    {
        if (itemBases.Count == 0)
        {
            return;
        }

        var ids = itemBases.Select(x => x.Id).ToArray();
        var existing = await _context.ItemBases
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = itemBases
            .Where(x => !existingSet.Contains(x.Id))
            .ToList();

        if (missing.Count > 0)
        {
            await _context.ItemBases.AddRangeAsync(missing, cancellationToken);
        }
    }
}
