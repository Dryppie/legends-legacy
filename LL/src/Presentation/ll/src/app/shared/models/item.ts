import { AttributeModifier } from './Dtos/attributesDto';
import { AttributeType } from './enums/attributeType';
import { EquipmentType } from './enums/equipmentType';
import { ItemType } from './enums/itemType';
import { Rarity } from './enums/rarity';
import { Essence } from './essence';

export interface ItemInstance {
  id: string;
  itemBase: ItemBase;
}

export interface EquipmentInstance extends ItemInstance {
  rarity: Rarity;
  equipmentBase: Equipment;
  potential?: number;
  itemXp: number;
  baseModifiers: AttributeModifier[];
  instanceModifiers: AttributeModifier[];
  attributeModifiers: AttributeModifier[];
}

export interface ItemBase {
  id: string;
  name: string;
  rarity: Rarity;
  itemType: ItemType;
  description: string;
  stackable: boolean;
  isBound?: boolean;
}

export interface Equipment extends ItemBase {
  equipmentType: EquipmentType;
  attributeModifiers: AttributeModifier[];
  attackSpeed: number;
  magnitude: number;
  magnitudeRange: number;
  scalingAttribute: AttributeType;
  scalingAmount: number;
}

export interface EssenceItem extends ItemBase {
  essence?: Essence;
  essenceDefinitionId: string;
  dismantleDustAmount: number;
}

export function essenceItemToEssence(item: EssenceItem): Essence {
  return (
    item.essence ?? {
      id: item.essenceDefinitionId || item.id,
      name: item.name,
      active: {
        name: 'Unbound Essence',
        description: item.description,
        attackTypes: [],
        damageTypes: [],
        effectTags: [],
        targeting: [],
        cooldown: 0,
        effects: [],
      },
      passive: {
        name: 'Soul Archive',
        description: 'Absorb this item to add it to the Soul Archive.',
        attackTypes: [],
        damageTypes: [],
        effectTags: [],
        targeting: [],
        cooldown: 0,
        effects: [],
      },
      attributeModifiers: [],
    }
  );
}
