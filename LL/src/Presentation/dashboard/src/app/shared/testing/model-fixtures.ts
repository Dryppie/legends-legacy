import { AttributeType } from '../models/enums/attributeType';
import { ItemType } from '../models/enums/itemType';
import { Rarity } from '../models/enums/rarity';
import { Essence } from '../models/essence';
import { ItemInstance } from '../models/item';
import { EquipmentType } from '../models/Dtos/equipmentSlot';

export const testAttribute = {
  attributeType: AttributeType.Power,
  value: 1,
};

export const testItem: ItemInstance = {
  id: 'test-item-instance',
  itemBase: {
    id: 'test-item',
    name: 'Test Item',
    rarity: Rarity.Common,
    itemType: ItemType.Equipment,
    description: 'Test item description',
    stackable: false,
    equipmentType: EquipmentType.OneHanded,
    attributeModifiers: [],
  },
};

export const testEssence: Essence = {
  name: 'Test Essence',
  active: {
    name: 'Test Active',
    effectTypes: [],
    description: 'Test active description',
    targeting: [],
    cooldown: 0,
  },
  passive: {
    name: 'Test Passive',
    effectTypes: [],
    description: 'Test passive description',
    targeting: [],
    cooldown: 0,
  },
  attributeModifiers: [],
};
