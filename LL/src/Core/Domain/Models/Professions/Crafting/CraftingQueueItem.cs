using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;
public class CraftingQueueItem
{
    public Guid Id { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Position { get; set; }
    public Guid EquipmentInstanceId { get; set; }
    public EquipmentInstance EquipmentInstance { get; set; } = null!;
    public CraftType CraftType { get; set; }
    public Guid CraftingActionDetailsId { get; set; }
}
