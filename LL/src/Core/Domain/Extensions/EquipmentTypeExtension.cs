using Domain.Models.Items.Equipments;

namespace Domain.Extensions;
public static class EquipmentTypeExtension
{
    public static bool IsWeaponType(this EquipmentType equipmentType)
        => equipmentType == EquipmentType.TwoHanded || equipmentType == EquipmentType.OneHanded || equipmentType == EquipmentType.OffHand;
}