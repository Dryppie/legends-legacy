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

describe('InventoryComponent', () => {
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
): InventoryComponent {
  return TestBed.runInInjectionContext(
    () =>
      new InventoryComponent(
        state,
        {
          pinnedOnboardingObjective: objective.asReadonly(),
        } as QuestStateService,
        modal,
        undefined,
        undefined,
        undefined,
        undefined,
        equipmentApi,
      ),
  );
}

function inventoryState(items: InventoryItem[]): InventoryStateService {
  return {
    load: jasmine.createSpy('load'),
    items: signal(items).asReadonly(),
    equipment: signal(items).asReadonly(),
  } as unknown as InventoryStateService;
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
