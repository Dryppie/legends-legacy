import { EquipmentInstance } from '../item';

export interface EquipmentSlot {
  id: string;
  iconPath: string;
  equipmentInstance?: EquipmentInstance;
  equipmentType: EquipmentType;
}

export enum EquipmentType {
  Head = 'Head',
  Cloak = 'Cloak',
  Chest = 'Chest',
  Necklace = 'Necklace',
  Legs = 'Legs',
  Ring = 'Ring',
  MainHand = 'MainHand',
  OffHand = 'OffHand',
}
