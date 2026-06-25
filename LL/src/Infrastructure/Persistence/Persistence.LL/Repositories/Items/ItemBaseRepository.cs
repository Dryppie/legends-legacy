using Application.Common.Interfaces;
using Domain.Models.Items;
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
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

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
