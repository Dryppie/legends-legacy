using Domain.Models.Items.Equipments.Slots;

namespace Domain.Extensions;
public static class EquipmentTypeExtension
{
    public static bool IsWeaponType(this EquipmentType equipmentType)
        => equipmentType == EquipmentType.MainHand || equipmentType == EquipmentType.OffHand;

}