import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  EquipmentService,
  EquipmentUpgradeMutation,
  EquipmentUpgradeQuote,
} from '../../../../core/services/api/equipment/equipment.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { ModifierType } from '../../../../shared/models/Dtos/attributesDto';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { ItemQuality } from '../../../../shared/models/enums/itemQuality';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../shared/models/enums/rarity';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { Equipment, EquipmentInstance } from '../../../../shared/models/item';
import { QuestObjectiveState } from '../../../../shared/models/quest';
import { InventoryComponent } from './inventory.component';
import { EquipmentLoadoutService } from '../../../../core/services/api/equipment/equipment-loadout.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';

describe('InventoryComponent', () => {
  it('clears the old equipped-item inspection when switching loadouts', () => {
    const selectedId = signal<string | null>('starter');
    const component = createComponent(
      inventoryState([]),
      undefined,
      undefined,
      undefined,
      { selectedId } as unknown as EquipmentLoadoutService,
    );
    TestBed.flushEffects();
    const item = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      1,
    );
    component.selectEquipmentSlot({
      id: 'main-hand',
      iconPath: '',
      equipmentSlotType: EquipmentSlotType.MainHand,
      equipmentInstance: item.itemInstance as EquipmentInstance,
    });
    expect(component.selectedItem()).not.toBeNull();

    selectedId.set('dungeon');
    TestBed.flushEffects();

    expect(component.selectedItem()).toBeNull();
    expect(component.selectedEquipmentSlot()).toBeNull();
    expect(component.selectedSlotEquipment()).toBeNull();
  });

  it('reuses the cached inventory snapshot when the page opens', () => {
    const state = inventoryState([]);
    const component = createComponent(state);

    component.ngOnInit();

    expect(state.load).toHaveBeenCalledOnceWith();
  });

  it('keeps the equipment collection selected for the equip objective', () => {
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = createComponent(inventoryState([]), objective);
    component.collectionView.set('Stock');

    objective.set({ type: 'EquipmentEquipped' } as QuestObjectiveState);
    TestBed.flushEffects();

    expect(component.collectionView()).toBe('Equipment');
  });

  it('sorts dropped equipment by quality and rank', () => {
    const standard = inventoryEquipment(
      'standard',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      4,
    );
    const masterpiece = inventoryEquipment(
      'masterpiece',
      EquipmentType.OneHanded,
      ItemQuality.Masterpiece,
      1,
    );
    const component = createComponent(inventoryState([standard, masterpiece]));

    component.setInventorySort('Quality');
    expect(component.filteredItems.map((item) => item.id)).toEqual([
      'masterpiece',
      'standard',
    ]);

    component.setInventorySort('Rank');
    expect(component.filteredItems.map((item) => item.id)).toEqual([
      'standard',
      'masterpiece',
    ]);
  });

  it('searches current quality, rank, and Blueprint Variant style metadata', () => {
    const item = inventoryEquipment(
      'styled',
      EquipmentType.OneHanded,
      ItemQuality.Fine,
      3,
      'blueprint_fury',
    );
    const component = createComponent(inventoryState([item]));

    for (const query of ['fine', 'rank 3', 'fury']) {
      component.inventorySearch = query;
      expect(component.filteredItems.map((entry) => entry.id)).toEqual([
        'styled',
      ]);
    }
  });

  it('filters equipment to types compatible with the selected slot', () => {
    const weapon = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      1,
    );
    const helmet = inventoryEquipment(
      'helmet',
      EquipmentType.Head,
      ItemQuality.Standard,
      1,
    );
    const component = createComponent(inventoryState([weapon, helmet]));

    component.selectEquipmentSlot({
      id: 'main-hand',
      iconPath: '',
      equipmentSlotType: EquipmentSlotType.MainHand,
    } as EquipmentSlot);

    expect(component.filteredItems.map((item) => item.id)).toEqual(['weapon']);
  });

  it('opens compact details for an equipped slot and keeps its filter when closed', () => {
    const weapon = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      1,
    );
    const component = createComponent(inventoryState([]));

    component.selectEquipmentSlot({
      id: 'main-hand',
      iconPath: '',
      equipmentSlotType: EquipmentSlotType.MainHand,
      equipmentInstance: weapon.itemInstance as EquipmentInstance,
    });

    expect(component.selectedItem()?.itemInstance.id).toBe('weapon');
    expect(component.mobileItemInspectorOpen()).toBeTrue();

    component.closeItemInspector();

    expect(component.mobileItemInspectorOpen()).toBeFalse();
    expect(component.selectedItem()).toBeNull();
    expect(component.selectedEquipmentSlot()).toBe(EquipmentSlotType.MainHand);
    expect(component.selectedSlotEquipment()?.id).toBe('weapon');
  });

  it('closes compact details when selecting an empty slot', () => {
    const weapon = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      1,
    );
    const component = createComponent(inventoryState([weapon]));
    component.handleInventoryItemClick(weapon);

    component.selectEquipmentSlot({
      id: 'head',
      iconPath: '',
      equipmentSlotType: EquipmentSlotType.Head,
    });

    expect(component.selectedEquipmentSlot()).toBe(EquipmentSlotType.Head);
    expect(component.selectedItem()).toBeNull();
    expect(component.mobileItemInspectorOpen()).toBeFalse();
  });

  for (const [equipmentType, slotType] of [
    [EquipmentType.OneHanded, EquipmentSlotType.MainHand],
    [EquipmentType.TwoHanded, EquipmentSlotType.MainHand],
    [EquipmentType.Head, EquipmentSlotType.Head],
  ] as const) {
    it(`compares ${equipmentType} gear with its equipped item without a slot filter`, () => {
      const candidate = inventoryEquipment(
        'candidate',
        equipmentType,
        ItemQuality.Standard,
        7,
      );
      const equipped = inventoryEquipment(
        'equipped',
        equipmentType,
        ItemQuality.Standard,
        13,
      );
      const component = createComponent(
        inventoryState([candidate]),
        undefined,
        undefined,
        undefined,
        undefined,
        equipmentState([equipmentSlot(slotType, equipped)]),
      );

      component.handleInventoryItemClick(candidate);

      expect(component.selectedEquipmentSlot()).toBeNull();
      expect(component.comparisonEquipmentFor(component.selectedItem()!)).toBe(
        equipped.itemInstance as EquipmentInstance,
      );
      expect(component.gearPowerDifference(candidate)).toBe(-6);
    });
  }

  it('uses an explicit off-hand filter and resumes automatic comparison after clearing it', () => {
    const candidate = inventoryEquipment(
      'candidate',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      7,
    );
    const mainHand = inventoryEquipment(
      'main-hand',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      13,
    );
    const offHand = inventoryEquipment(
      'off-hand',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      4,
    );
    const offHandSlot = equipmentSlot(EquipmentSlotType.OffHand, offHand);
    const component = createComponent(
      inventoryState([candidate]),
      undefined,
      undefined,
      undefined,
      undefined,
      equipmentState([
        offHandSlot,
        equipmentSlot(EquipmentSlotType.MainHand, mainHand),
      ]),
    );

    component.selectEquipmentSlot(offHandSlot);
    component.handleInventoryItemClick(candidate);
    expect(component.comparisonEquipmentFor(candidate)).toBe(
      offHand.itemInstance as EquipmentInstance,
    );
    expect(component.gearPowerDifference(candidate)).toBe(3);

    component.clearEquipmentSlotFilter();
    component.handleInventoryItemClick(candidate);
    expect(component.comparisonEquipmentFor(candidate)).toBe(
      mainHand.itemInstance as EquipmentInstance,
    );
    expect(component.gearPowerDifference(candidate)).toBe(-6);
  });

  it('does not compare with unrelated gear when the matching equipment slot is empty', () => {
    const helmet = inventoryEquipment(
      'helmet',
      EquipmentType.Head,
      ItemQuality.Standard,
      7,
    );
    const weapon = inventoryEquipment(
      'weapon',
      EquipmentType.TwoHanded,
      ItemQuality.Standard,
      13,
    );
    const component = createComponent(
      inventoryState([helmet]),
      undefined,
      undefined,
      undefined,
      undefined,
      equipmentState([equipmentSlot(EquipmentSlotType.MainHand, weapon)]),
    );

    component.handleInventoryItemClick(helmet);

    expect(component.comparisonEquipmentFor(helmet)).toBeNull();
    expect(component.gearPowerDifference(helmet)).toBeNull();
  });

  it('does not compare an already equipped weapon against itself or the other hand', () => {
    const mainHand = inventoryEquipment(
      'main-hand',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      13,
    );
    const offHand = inventoryEquipment(
      'off-hand',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      4,
    );
    const component = createComponent(
      inventoryState([]),
      undefined,
      undefined,
      undefined,
      undefined,
      equipmentState([
        equipmentSlot(EquipmentSlotType.MainHand, mainHand),
        equipmentSlot(EquipmentSlotType.OffHand, offHand),
      ]),
    );

    expect(component.comparisonEquipmentFor(offHand)).toBeNull();
    expect(component.gearPowerDifference(offHand)).toBeNull();
  });

  it('opens reinforcement management for current personally-owned equipment', () => {
    const item = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      0,
    );
    const modal = jasmine.createSpyObj<ModalService>('ModalService', [
      'toggleInventoryEquipItemModal',
    ]);
    const component = createComponent(inventoryState([item]), undefined, modal);

    expect(component.canManageEquipment(item)).toBeTrue();
    component.manageEquipment(item);

    expect(modal.toggleInventoryEquipItemModal).toHaveBeenCalledOnceWith(
      item.itemInstance as EquipmentInstance,
      'manage',
    );
  });

  it('dismantles the selected equipment only after confirmation', () => {
    const item = inventoryEquipment(
      'weapon',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      0,
    );
    const quote = {
      canExecute: true,
      partsReturned: 2,
      request: { itemInstanceId: item.itemInstance.id },
    } as EquipmentUpgradeQuote;
    const equipmentApi = jasmine.createSpyObj<EquipmentService>(
      'EquipmentService',
      ['previewUpgrade', 'dismantle'],
    );
    equipmentApi.previewUpgrade.and.returnValue(of(quote));
    equipmentApi.dismantle.and.returnValue(
      of({ outcome: {} } as unknown as EquipmentUpgradeMutation),
    );
    const state = inventoryState([item]);
    const component = createComponent(
      state,
      undefined,
      undefined,
      equipmentApi,
    );

    component.selectInventoryItem(item);
    component.requestSelectedDismantle(item);
    expect(equipmentApi.dismantle).not.toHaveBeenCalled();

    component.requestSelectedDismantle(item);

    expect(equipmentApi.dismantle).toHaveBeenCalledOnceWith(quote);
    expect(state.load).toHaveBeenCalledOnceWith(true);
  });

  it('mass dismantles shown equipment at or below the selected rarity while protecting favorites', () => {
    const common = inventoryEquipment(
      'common',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      0,
    );
    const favorite = inventoryEquipment(
      'favorite',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      0,
    );
    favorite.isFavorite = true;
    const rare = inventoryEquipment(
      'rare',
      EquipmentType.OneHanded,
      ItemQuality.Standard,
      0,
    );
    (rare.itemInstance as EquipmentInstance).rarity = Rarity.Rare;
    const equipmentApi = jasmine.createSpyObj<EquipmentService>(
      'EquipmentService',
      ['previewUpgrade', 'dismantle'],
    );
    equipmentApi.previewUpgrade.and.callFake((itemInstanceId) =>
      of({
        canExecute: true,
        request: { itemInstanceId },
      } as EquipmentUpgradeQuote),
    );
    equipmentApi.dismantle.and.returnValue(
      of({ outcome: {} } as unknown as EquipmentUpgradeMutation),
    );
    const state = inventoryState([common, favorite, rare]);
    const component = createComponent(
      state,
      undefined,
      undefined,
      equipmentApi,
    );

    expect(component.massDismantleCandidates.map((item) => item.id)).toEqual([
      'common',
    ]);
    component.selectMassDismantleRarity(Rarity.Rare);
    expect(component.massDismantleCandidates.map((item) => item.id)).toEqual([
      'common',
      'rare',
    ]);
    component.selectMassDismantleRarity(Rarity.Common);
    component.requestMassDismantle();
    component.requestMassDismantle();

    expect(equipmentApi.previewUpgrade).toHaveBeenCalledOnceWith(
      common.itemInstance.id,
      'Dismantle',
    );
    expect(equipmentApi.dismantle).toHaveBeenCalledTimes(1);
    expect(component.massDismantleStatus()).toBe('Dismantled 1 item.');
  });
});

function createComponent(
  state: InventoryStateService,
  objective = signal<QuestObjectiveState | undefined>(undefined),
  modal?: ModalService,
  equipmentApi?: EquipmentService,
  loadoutState?: EquipmentLoadoutService,
  equippedState?: EquipmentStateService,
): InventoryComponent {
  return TestBed.runInInjectionContext(
    () =>
      new InventoryComponent(
        state,
        {
          pinnedOnboardingObjective: objective.asReadonly(),
        } as QuestStateService,
        modal,
        equippedState,
        undefined,
        undefined,
        undefined,
        equipmentApi,
        loadoutState,
      ),
  );
}

function inventoryState(items: InventoryItem[]): InventoryStateService {
  return {
    load: jasmine.createSpy('load'),
    isFavorite: () => false,
    items: signal(items).asReadonly(),
    equipment: signal(items).asReadonly(),
  } as unknown as InventoryStateService;
}

function equipmentState(slots: EquipmentSlot[]): EquipmentStateService {
  return {
    equipmentSlots: signal(slots).asReadonly(),
    getSlot: (slotType: EquipmentSlotType) =>
      slots.find((slot) => slot.equipmentSlotType === slotType),
  } as unknown as EquipmentStateService;
}

function equipmentSlot(
  equipmentSlotType: EquipmentSlotType,
  item: InventoryItem,
): EquipmentSlot {
  return {
    id: equipmentSlotType,
    iconPath: '',
    equipmentSlotType,
    equipmentInstance: item.itemInstance as EquipmentInstance,
  };
}

function inventoryEquipment(
  id: string,
  equipmentType: EquipmentType,
  quality: ItemQuality,
  rank: number,
  activeStyleId: string | null = null,
): InventoryItem {
  const equipmentBase: Equipment = {
    id: `${id}-base`,
    name: id,
    description: '',
    itemType: ItemType.Equipment,
    rarity: Rarity.Common,
    stackable: false,
    equipmentType,
    attributeModifiers: [],
    itemBudget: rank,
    itemBudgetTier: 1,
  };
  const equipment: EquipmentInstance = {
    id,
    itemBase: equipmentBase,
    displayName: id,
    rarity: Rarity.Common,
    quality,
    tier: 1,
    equipmentBase,
    baseModifiers: [
      {
        attributeType: AttributeType.Power,
        amount: rank,
        modifierType: ModifierType.Flat,
      },
    ],
    instanceModifiers: [],
    attributeModifiers: [],
    affinityTags: [],
    itemBudget: rank,
    itemBudgetTier: 1,
    isGuildBorrowed: false,
    progression: {
      modelVersion: 1,
      balanceVersion: 1,
      definitionId: equipmentBase.id,
      archetypeId: equipmentType,
      rank,
      quality,
      attributeRollMultiplier: 1,
      nativeStyleId: activeStyleId,
      activeStyleId,
      ownership: 'UnboundPersonal',
    },
  };

  return { id, itemInstance: equipment, quantity: 1 };
}
