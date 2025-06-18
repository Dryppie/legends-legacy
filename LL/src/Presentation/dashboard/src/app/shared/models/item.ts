import { AttributeModifier } from './Dtos/attributesDto';
import { EquipmentType } from './Dtos/equipmentSlot';
import { AttributeType } from './enums/attributeType';
import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
}

export interface EquipmentInstance extends ItemInstance {}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
  magnitude: number;
  scalingAttribute: AttributeType;
  scalingAmount: number;
}

export interface EssenceItem extends ItemBase {
  essence: Essence;
}
