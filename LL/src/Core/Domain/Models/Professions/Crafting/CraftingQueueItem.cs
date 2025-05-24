using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;
public class CraftingQueueItem
{
    public Guid Id { get; set; }
    public byte QueueIndex { get; set; }
    public Guid EquipmentInstanceId { get; set; }
    public EquipmentInstance EquipmentInstance { get; set; } = null!;
    public Guid CraftingActionDetailsId { get; set; }
}