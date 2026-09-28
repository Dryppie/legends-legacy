using Domain.Models.Analytics;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Analytics;

public sealed class ItemizationChoiceRepository(LLDbContext db) : IItemizationChoiceRepository
{
    public async Task<ItemizationChoiceInventory?> CaptureAsync(Guid characterId, CancellationToken ct)
    {
        // Tracking preserves earlier inventory/slot changes within this command transaction.
        var character = await db.Characters.AsSplitQuery()
            .Include(x => x.Inventory).ThenInclude(x => x.InventoryItems).ThenInclude(x => x.ItemInstance)
            .Include(x => x.EquipmentSlots).ThenInclude(x => x.EquipmentInstance)
            .SingleOrDefaultAsync(x => x.Id == characterId, ct);
        if (character?.Inventory is null) return null;
        var loans = await db.GuildVaultItems.Where(x => x.BorrowedByCharacterId == characterId
                && db.GuildMembers.Any(m => m.GuildId == x.GuildId && m.CharacterId == characterId))
            .Select(x => new { x.EquipmentInstanceId, x.GuildId }).ToListAsync(ct);
        var items = character.Inventory.InventoryItems.Where(x => x.Quantity > 0)
            .Select(x => x.ItemInstance).OfType<EquipmentInstance>().Select(x => (Item: x, Equipped: false))
            .Concat(character.EquipmentSlots.Where(x => x.EquipmentInstance is not null)
                .Select(x => (Item: x.EquipmentInstance!, Equipped: true))).ToArray();
        var validLoans = items.Where(x => loans.Any(l => l.EquipmentInstanceId == x.Item.Id
                && l.GuildId == x.Item.ProgressionData?.State.Ownership.OwnerId))
            .Select(x => x.Item.Id).ToHashSet();
        return ItemizationChoiceInventory.Capture(characterId, character.Level,
            character.EquipmentSlots.Select(x => x.EquipmentSlotType).Distinct().ToArray(), items, validLoans);
    }
}
