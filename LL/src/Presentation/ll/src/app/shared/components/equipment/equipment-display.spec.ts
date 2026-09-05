import { ModifierType } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { Equipment, EquipmentInstance } from '../../models/item';
import {
  buildAttributeComparisons,
  mapEquipmentToDisplay,
  mapInstanceToDisplay,
} from './equipment-display';

describe('equipment display mapping', () => {
  it('maps catalog equipment without retired crafting metadata', () => {
    const display = mapEquipmentToDisplay(equipmentBase());

    expect(display.name).toBe('Iron Sword');
    expect(display.attributes).toEqual([
      {
        attributeType: AttributeType.Power,
        amount: 8,
        modifierType: ModifierType.Flat,
      },
    ]);
    expect(display.requiredLevel).toBe(1);
  });

  it('preserves dropped equipment quality, rank, style, and ownership', () => {
    const instance = equipmentInstance();

    const display = mapInstanceToDisplay(instance);

    expect(display.quality).toBe(ItemQuality.Exceptional);
    expect(display.progression?.rank).toBe(3);
    expect(display.progression?.activeStyleId).toBe('style.fury');
    expect(display.progression?.ownership).toBe('UnboundPersonal');
  });

  it('prefers split base and instance modifiers over the compatibility list', () => {
    const instance = equipmentInstance();
    instance.baseModifiers = [modifier(AttributeType.Power, 10)];
    instance.instanceModifiers = [modifier(AttributeType.Power, 2)];
    instance.attributeModifiers = [modifier(AttributeType.Power, 99)];

    expect(mapInstanceToDisplay(instance).attributes[0].amount).toBe(12);
  });

  it('compares every attribute present on either item', () => {
    const hovered = mapInstanceToDisplay(equipmentInstance());
    const equipped = mapInstanceToDisplay(equipmentInstance());
    hovered.attributes = [modifier(AttributeType.Power, 12)];
    equipped.attributes = [modifier(AttributeType.Armor, 4)];

    expect(buildAttributeComparisons(hovered, equipped)).toEqual([
      {
        attributeType: AttributeType.Power,
        equippedAmount: 0,
        hoveredAmount: 12,
      },
      {
        attributeType: AttributeType.Armor,
        equippedAmount: 4,
        hoveredAmount: 0,
      },
    ]);
  });
});

function equipmentBase(): Equipment {
  return {
    id: 'equipment.iron-sword',
    name: 'Iron Sword',
    description: '',
    itemType: ItemType.Equipment,
    rarity: Rarity.Rare,
    stackable: false,
    equipmentType: EquipmentType.OneHanded,
    attributeModifiers: [modifier(AttributeType.Power, 8)],
    itemBudget: 8,
    itemBudgetTier: 1,
  };
}

function equipmentInstance(): EquipmentInstance {
  const base = equipmentBase();
  return {
    id: 'equipment-instance',
    itemBase: base,
    displayName: 'Furious Iron Sword',
    rarity: Rarity.Rare,
    quality: ItemQuality.Exceptional,
    tier: 2,
    requiredLevel: 10,
    equipmentBase: base,
    baseModifiers: [modifier(AttributeType.Power, 8)],
    instanceModifiers: [modifier(AttributeType.Power, 2)],
    attributeModifiers: [],
    affinityTags: [],
    itemBudget: 10,
    itemBudgetTier: 2,
    isGuildBorrowed: false,
    progression: {
      modelVersion: 1,
      balanceVersion: 1,
      definitionId: 'equipment.iron-sword',
      archetypeId: 'one-handed',
      rank: 3,
      quality: ItemQuality.Exceptional,
      attributeRollMultiplier: 1.04,
      nativeStyleId: 'style.fury',
      activeStyleId: 'style.fury',
      ownership: 'UnboundPersonal',
    },
  };
}

function modifier(attributeType: AttributeType, amount: number) {
  return { attributeType, amount, modifierType: ModifierType.Flat };
}
