using Domain.Models.Entities;

namespace Domain.Models.Essences.EssenceSlots;
public class EssenceSlot
{
    public Guid Id { get; set; }
    /// <summary>
    ///  What type of slot is this (Standard vs. Subscription-based)?
    /// </summary>
    public SlotType SlotType { get; set; }

    /// <summary>
    ///  Current state of the slot (Active, Reserved, or Locked).
    /// </summary>
    public SlotState SlotState { get; set; }
    public Guid EntityId { get; set; }
    public Guid EssenceId { get; set; }
    /// <summary>
    ///  Which Essence (if any) is currently in this slot?
    /// </summary>
    public Essence? OccupiedEssence { get; set; }
}
