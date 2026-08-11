import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../models/enums/equipmentType';
import { Equipment, EquipmentInstance } from '../../models/item';

export interface EquippedComparison {
  slotType: EquipmentSlotType;
  equipmentInstance: EquipmentInstance;
}

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
    case EquipmentSlotType.Tool:
      return [EquipmentType.Tool];
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
    case EquipmentType.Tool:
      return EquipmentSlotType.Tool;
    default:
      throw new Error(`Unhandled equipment type: ${equipmentType}`);
  }
}

export function findEquippedComparison(
  item: Equipment | EquipmentInstance,
  slots: readonly EquipmentSlot[],
): EquipmentInstance | null {
  return findEquippedComparisons(item, slots)[0]?.equipmentInstance ?? null;
}

export function findEquippedComparisons(
  item: Equipment | EquipmentInstance,
  slots: readonly EquipmentSlot[],
): EquippedComparison[] {
  if (
    isEquipmentInstance(item) &&
    slots.some((slot) => slot.equipmentInstance?.id === item.id)
  ) {
    return [];
  }

  const equipmentType = isEquipmentInstance(item)
    ? item.equipmentBase.equipmentType
    : item.equipmentType;
  const comparisonSlots = getComparisonSlots(equipmentType);
  const comparisons: EquippedComparison[] = [];
  const includedItemIds = new Set<string>();

  for (const slotType of comparisonSlots) {
    const equipped = slots.find(
      (slot) => slot.equipmentSlotType === slotType,
    )?.equipmentInstance;

    if (!equipped || includedItemIds.has(equipped.id)) continue;
    if (
      equipmentType === EquipmentType.OffHand &&
      slotType === EquipmentSlotType.MainHand &&
      equipped.equipmentBase.equipmentType !== EquipmentType.TwoHanded
    ) {
      continue;
    }

    comparisons.push({ slotType, equipmentInstance: equipped });
    includedItemIds.add(equipped.id);
  }

  return comparisons;
}

function getComparisonSlots(equipmentType: EquipmentType): EquipmentSlotType[] {
  switch (equipmentType) {
    case EquipmentType.OneHanded:
    case EquipmentType.TwoHanded:
      return [EquipmentSlotType.MainHand, EquipmentSlotType.OffHand];
    case EquipmentType.OffHand:
      return [EquipmentSlotType.OffHand, EquipmentSlotType.MainHand];
    default:
      return [getSlotTypeFromEquipmentType(equipmentType)];
  }
}

function isEquipmentInstance(
  item: Equipment | EquipmentInstance,
): item is EquipmentInstance {
  return 'equipmentBase' in item;
}
