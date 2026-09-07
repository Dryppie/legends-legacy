using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments.Loadouts;

public interface IEquipmentLoadoutRepository
{
    Task<List<EquipmentLoadout>> GetAsync(Guid characterId, CancellationToken ct);
    Task<HashSet<Guid>> GetAvailableItemIdsAsync(Guid characterId, CancellationToken ct);
    Task AddAsync(EquipmentLoadout loadout, CancellationToken ct);
    void ReplaceSlots(EquipmentLoadout loadout, IReadOnlyCollection<EquipmentSlot> slots);
    void Remove(EquipmentLoadout loadout);
}
