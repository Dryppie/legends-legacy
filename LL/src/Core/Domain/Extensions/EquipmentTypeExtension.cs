using Domain.Models.Items.Equipments;

namespace Domain.Extensions;
public static class EquipmentTypeExtension
{
    public static bool IsWeaponType(this EquipmentType equipmentType)
        => equipmentType == EquipmentType.TwoHandedWeapon || equipmentType == EquipmentType.OneHandedWeapon || equipmentType == EquipmentType.OffHand;
}