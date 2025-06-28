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

export function getSlotTypeFromEquipmentType(
  equipmentType: EquipmentType,
  ): EquipmentSlotType {
    switch (equipmentType) {
      case EquipmentType.Head:
        return EquipmentSlotType.Head;
      case EquipmentType.Chest:
        return EquipmentSlotType.Chest;
      case EquipmentType.Legs:
        return EquipmentSlotType.Legs;
      case EquipmentType.Relic:
        return EquipmentSlotType.Relic;
      case EquipmentType.Necklace:
        return EquipmentSlotType.Necklace;
      case EquipmentType.Ring:
        return EquipmentSlotType.Ring;
      case EquipmentType.TwoHanded:
        return EquipmentSlotType.MainHand;
      case EquipmentType.OneHanded:
        return EquipmentSlotType.MainHand;
      case EquipmentType.OffHand:
        return EquipmentSlotType.OffHand;
      default:
        throw new Error(`Unhandled equipment type: ${equipmentType}`);
    }
  }
