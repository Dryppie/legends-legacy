namespace Domain.Models.Items.Equipments;
public class EquipmentInstance : ItemInstance
{
    public int? Potential { get; set; } = null;
    public int ItemXp { get; set; } = 0;
    public bool IsMasterpiece { get; set; } = false;
    public bool IsLevelingItem { get; set; } = false;
}