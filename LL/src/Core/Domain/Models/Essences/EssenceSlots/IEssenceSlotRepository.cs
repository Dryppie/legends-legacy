namespace Domain.Models.Essences.EssenceSlots;
public interface IEssenceSlotRepository
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
    Task<bool> ToggleActiveReserved(Guid entityId, Guid essenceSlotId, CancellationToken cancellationToken);
    void LockSlot();
    void UnlockSlot();
}
