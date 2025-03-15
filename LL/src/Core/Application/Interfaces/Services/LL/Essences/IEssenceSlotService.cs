using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;

namespace Application.Interfaces.Services.LL.Essences;
public interface IEssenceSlotService
{
    /// <summary>
    ///  Equip an Essence into a slot (only if not locked).
    /// </summary>
    Task<bool> EquipEssence(Essence essence);

    /// <summary>
    ///  Unequip an Essence from a slot, even if reserved (but not if locked).
    /// </summary>
    Task<bool> UnequipEssence();

    /// <summary>
    ///  Switch this slot’s active/reserved state if it’s not locked.
    /// </summary>
    Task<bool> ToggleActiveReserved(Guid characterId, Guid essenceSlotId, CancellationToken cancellationToken);

    /// <summary>
    ///  Lock slots
    /// </summary>
    Task LockSlot();

    /// <summary>
    ///  Switch this slot’s active/reserved state if it’s not locked.
    /// </summary>
    Task UnlockSlot();

    /// <summary>
    /// Create a new essence slot for a character - Either Active or Reserved - Upon leveling
    /// </summary>
    Task CreateEssenceSlotOnLevelUp(Guid characterId, SlotState slotState, CancellationToken cancellationToken);
}
