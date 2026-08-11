import { EquipmentSlotType } from '../../models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../models/enums/equipmentType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { Equipment, EquipmentInstance } from '../../models/item';
import {
  findEquippedComparison,
  findEquippedComparisons,
} from './equipment.utils';

describe('equipment hover comparison', () => {
  it('finds the equipped item in the hovered gear slot', () => {
    const equipped = equipmentInstance('equipped-chest', EquipmentType.Chest);
    const hovered = equipmentInstance('hovered-chest', EquipmentType.Chest);

    expect(
      findEquippedComparison(hovered, [
        {
          id: 'chest-slot',
          iconPath: 'chest',
          equipmentSlotType: EquipmentSlotType.Chest,
          equipmentInstance: equipped,
        },
      ]),
    ).toBe(equipped);
  });

  it('does not compare gear that is already equipped in any slot', () => {
    const equippedOffHand = equipmentInstance(
      'equipped-one-handed',
      EquipmentType.OneHanded,
    );

    expect(
      findEquippedComparison(equippedOffHand, [
        {
          id: 'off-hand-slot',
          iconPath: 'off-hand',
          equipmentSlotType: EquipmentSlotType.OffHand,
          equipmentInstance: equippedOffHand,
        },
      ]),
    ).toBeNull();
  });

  it('compares one-handed and two-handed weapons with both equipped hands', () => {
    const equippedMainHand = equipmentInstance(
      'equipped-main-hand',
      EquipmentType.OneHanded,
    );
    const equippedOffHand = equipmentInstance(
      'equipped-off-hand',
      EquipmentType.OffHand,
    );
    const slots = [
      equipmentSlot(
        'main-hand-slot',
        EquipmentSlotType.MainHand,
        equippedMainHand,
      ),
      equipmentSlot(
        'off-hand-slot',
        EquipmentSlotType.OffHand,
        equippedOffHand,
      ),
    ];

    for (const hoveredType of [
      EquipmentType.OneHanded,
      EquipmentType.TwoHanded,
    ]) {
      expect(
        findEquippedComparisons(
          equipmentInstance(`hovered-${hoveredType}`, hoveredType),
          slots,
        ),
      ).toEqual([
        {
          slotType: EquipmentSlotType.MainHand,
          equipmentInstance: equippedMainHand,
        },
        {
          slotType: EquipmentSlotType.OffHand,
          equipmentInstance: equippedOffHand,
        },
      ]);
    }
  });

  it('compares off-hand gear only with the equipped off-hand', () => {
    const equippedMainHand = equipmentInstance(
      'equipped-main-hand',
      EquipmentType.OneHanded,
    );
    const equippedOffHand = equipmentInstance(
      'equipped-off-hand',
      EquipmentType.OffHand,
    );

    expect(
      findEquippedComparisons(
        equipmentInstance('hovered-off-hand', EquipmentType.OffHand),
        [
          equipmentSlot(
            'main-hand-slot',
            EquipmentSlotType.MainHand,
            equippedMainHand,
          ),
          equipmentSlot(
            'off-hand-slot',
            EquipmentSlotType.OffHand,
            equippedOffHand,
          ),
        ],
      ),
    ).toEqual([
      {
        slotType: EquipmentSlotType.OffHand,
        equipmentInstance: equippedOffHand,
      },
    ]);
  });

  it('compares off-hand gear with an equipped two-handed weapon', () => {
    const equippedTwoHanded = equipmentInstance(
      'equipped-two-handed',
      EquipmentType.TwoHanded,
    );

    expect(
      findEquippedComparisons(
        equipmentInstance('hovered-off-hand', EquipmentType.OffHand),
        [
          equipmentSlot(
            'main-hand-slot',
            EquipmentSlotType.MainHand,
            equippedTwoHanded,
          ),
        ],
      ),
    ).toEqual([
      {
        slotType: EquipmentSlotType.MainHand,
        equipmentInstance: equippedTwoHanded,
      },
    ]);
  });
});

function equipmentSlot(
  id: string,
  equipmentSlotType: EquipmentSlotType,
  equipmentInstance: EquipmentInstance,
) {
  return {
    id,
    iconPath: equipmentSlotType,
    equipmentSlotType,
    equipmentInstance,
  };
}

function equipmentInstance(
  id: string,
  equipmentType: EquipmentType,
): EquipmentInstance {
  const equipmentBase = {
    id: `${id}-base`,
    name: id,
    description: '',
    itemType: ItemType.Equipment,
    rarity: Rarity.Common,
    stackable: false,
    equipmentType,
    attributeModifiers: [],
    itemBudget: 0,
    itemBudgetTier: 1,
  } satisfies Equipment;

  return {
    id,
    itemBase: equipmentBase,
    displayName: id,
    rarity: Rarity.Common,
    quality: ItemQuality.Standard,
    tier: 1,
    equipmentBase,
    temperingProgress: 0,
    itemXp: 0,
    baseModifiers: [],
    instanceModifiers: [],
    attributeModifiers: [],
    toolAffixes: [],
    effectiveToolBonuses: [],
    affinityTags: [],
    itemBudget: 0,
    itemBudgetTier: 1,
    isGuildBorrowed: false,
  };
}
