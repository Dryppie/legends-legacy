using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments.Loadouts;

public sealed class EquipmentLoadout
{
    public const int Limit = 3;
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PresetSlot { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsUsable { get; set; } = true;
    public EssenceCombatActivity AutoUseActivities { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<EquipmentLoadoutSlot> Slots { get; set; } = [];
}

public sealed class EquipmentLoadoutSlot
{
    public Guid Id { get; set; }
    public Guid EquipmentLoadoutId { get; set; }
    public EquipmentSlotType SlotType { get; set; }
    public Guid? EquipmentInstanceId { get; set; }
    public EquipmentInstance? EquipmentInstance { get; set; }
}
