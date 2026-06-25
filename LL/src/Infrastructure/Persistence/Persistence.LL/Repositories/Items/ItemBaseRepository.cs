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

        return await _context.ItemBases
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
}
