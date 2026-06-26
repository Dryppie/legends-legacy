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

    public async Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<string, ItemBase>();
        }

        await NormalizePlainItemBaseDiscriminatorsAsync(itemIds, cancellationToken);

        return await _context.ItemBases
            .Include(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => (x as EquipmentBase)!.ToolBonuses)
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken)
    {
        var essenceItems = await _context.ItemBases
            .OfType<EssenceItemBase>()
            .Where(item => !string.IsNullOrWhiteSpace(item.EssenceDefinitionId))
            .Select(item => new { item.Id, item.EssenceDefinitionId })
            .ToListAsync(cancellationToken);

        return essenceItems
            .GroupBy(x => x.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase).First().Id,
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) =>
        await _context.ItemBases
            .OfType<EquipmentBase>()
            .Include(x => x.AttributeModifiers)
            .FirstOrDefaultAsync(x => x.Id == itemBaseId, cancellationToken);

    public async Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken)
    {
        if (itemBases.Count == 0)
        {
            return;
        }

        var ids = itemBases.Select(x => x.Id).ToArray();
        await NormalizePlainItemBaseDiscriminatorsAsync(ids, cancellationToken);

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

    private async Task NormalizePlainItemBaseDiscriminatorsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext || !dbContext.Database.IsRelational())
        {
            return;
        }

        foreach (var itemId in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _context.ExecuteSqlRawAsync(
                """UPDATE "ItemBases" SET "ItemType" = {0} WHERE "Id" = {1} AND "ItemType" = {2}""",
                cancellationToken,
                (int)ItemType.Resource,
                itemId,
                (int)ItemType.Consumable);
        }
    }
}
