using Domain.Models.Essences.EssenceSlots;

namespace Domain.Extensions;
public static class EssenceSlotExtension
{
    /// <summary>
    /// Chainable method that returns a list of active essenceSlots with an occupied Essence
    /// </summary>
    /// <param name="slots"></param>
    /// <returns></returns>
    public static IEnumerable<EssenceSlot> ActiveSlotsWithEssences(this IEnumerable<EssenceSlot> slots) =>
        slots.Where(s => s.OccupiedEssence != null && s.SlotState == SlotState.Active);
}
