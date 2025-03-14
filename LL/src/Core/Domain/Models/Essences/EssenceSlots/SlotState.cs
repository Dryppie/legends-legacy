namespace Domain.Models.Essences.EssenceSlots;
public enum SlotState
{
    Active,    // Essence is actively usable
    Reserved,  // Essence is dormant, not usable
    Locked     // Slot is locked and cannot be used (e.g. subscription expired)
}
