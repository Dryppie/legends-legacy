import { EquipmentSlotType } from '../../models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../models/enums/equipmentType';

export function getAllowedEquipmentTypesForSlot(
  slot: EquipmentSlotType,
): EquipmentType[] {
  switch (slot) {
    case EquipmentSlotType.MainHand:
      return [EquipmentType.OneHanded, EquipmentType.TwoHanded];
    case EquipmentSlotType.OffHand:
      return [EquipmentType.OneHanded, EquipmentType.OffHand];
    case EquipmentSlotType.Head:
      return [EquipmentType.Head];
    case EquipmentSlotType.Chest:
      return [EquipmentType.Chest];
    case EquipmentSlotType.Legs:
      return [EquipmentType.Legs];
    case EquipmentSlotType.Relic:
      return [EquipmentType.Relic];
    case EquipmentSlotType.Necklace:
      return [EquipmentType.Necklace];
    case EquipmentSlotType.Ring:
      return [EquipmentType.Ring];
    default:
      return [];
  }
}
