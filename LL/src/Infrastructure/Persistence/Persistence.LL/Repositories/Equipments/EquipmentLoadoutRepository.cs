using Application.Common.Interfaces;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Loadouts;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentLoadoutRepository(IDbContext context) : IEquipmentLoadoutRepository
{
    public Task<List<EquipmentLoadout>> GetAsync(Guid characterId, CancellationToken ct) =>
        context.EquipmentLoadouts
            .Include(x => x.Slots).ThenInclude(x => x.EquipmentInstance).ThenInclude(x => x!.ItemBase)
            .Include(x => x.Slots).ThenInclude(x => x.EquipmentInstance).ThenInclude(x => x!.InstanceModifiers)
            .Where(x => x.CharacterId == characterId).OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(ct);

    public async Task<HashSet<Guid>> GetAvailableItemIdsAsync(Guid characterId, CancellationToken ct)
    {
        var character = await context.Characters.Where(x => x.Id == characterId)
            .Select(x => new { x.Level }).SingleOrDefaultAsync(ct);
        if (character is null) return [];
        var items = await context.ItemInstances.OfType<EquipmentInstance>().Where(item =>
            context.InventoryItems.Any(x => x.InventoryId == characterId && x.ItemInstanceId == item.Id && x.Quantity > 0) ||
            context.EquipmentSlots.Any(x => x.EntityId == characterId && x.EquipmentInstanceId == item.Id))
            .ToListAsync(ct);
        var loans = await context.GuildVaultItems.Where(x => x.BorrowedByCharacterId == characterId &&
            context.GuildMembers.Any(m => m.GuildId == x.GuildId && m.CharacterId == characterId))
            .Select(x => x.EquipmentInstanceId).ToListAsync(ct);
        return EquipmentLoadoutAvailability.GetAvailableItemIds(character.Level, characterId, items, loans);
    }

    public async Task AddAsync(EquipmentLoadout loadout, CancellationToken ct) => await context.EquipmentLoadouts.AddAsync(loadout, ct);
    public void Remove(EquipmentLoadout loadout) => context.EquipmentLoadouts.Remove(loadout);
    public void ReplaceSlots(EquipmentLoadout loadout, IReadOnlyCollection<EquipmentSlot> slots)
    {
        context.EquipmentLoadoutSlots.RemoveRange(loadout.Slots);
        loadout.Slots = slots.Where(x => x.EquipmentInstanceId.HasValue).Select(x => new EquipmentLoadoutSlot
        {
            Id = Guid.NewGuid(), EquipmentLoadoutId = loadout.Id, SlotType = x.EquipmentSlotType,
            EquipmentInstanceId = x.EquipmentInstanceId, EquipmentInstance = x.EquipmentInstance
        }).ToList();
        context.EquipmentLoadoutSlots.AddRange(loadout.Slots);
    }
}
