import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { QuestPresenterService } from '../../../../core/services/api/quest/quest-presenter.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { ItemQuality } from '../../../../shared/models/enums/itemQuality';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../shared/models/enums/rarity';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { QuestObjectiveState } from '../../../../shared/models/quest';
import { InventoryComponent } from './inventory.component';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';

describe('InventoryComponent quest presentation', () => {
  it('starts gathering-tool guidance when that objective becomes active', () => {
    const objective = signal<QuestObjectiveState | undefined>({
      key: 'equip_gathering_tool',
      description: 'Equip a gathering tool.',
      type: 'GatheringToolEquipped',
      currentAmount: 0,
      requiredAmount: 1,
      isCompleted: false,
      presentation: {
        actionLabel: 'Open Inventory',
        destinationRoute: '/game/character/inventory',
        tourPageId: 'tutorial-gathering-tool',
      },
    });
    const presenter = jasmine.createSpyObj<QuestPresenterService>(
      'QuestPresenterService',
      ['presentCurrentObjective'],
    );

    TestBed.configureTestingModule({});
    TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          {} as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          presenter,
        ),
    );
    TestBed.flushEffects();

    expect(presenter.presentCurrentObjective).toHaveBeenCalledOnceWith();
  });

  it('excludes tools from scrap mode', () => {
    const equipment = signal<InventoryItem[]>([
      inventoryEquipment('weapon', EquipmentType.OneHanded),
      inventoryEquipment('tool', EquipmentType.Tool),
    ]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);

    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          { equipment: equipment.asReadonly() } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    expect(
      component.scrapableEquipment().map((item) => item.itemInstance.id),
    ).toEqual(['weapon']);
  });

  it('sorts scrap equipment by quality from highest to lowest', () => {
    const equipment = signal<InventoryItem[]>([
      inventoryEquipment('standard', EquipmentType.Head, ItemQuality.Standard),
      inventoryEquipment(
        'masterwork',
        EquipmentType.Head,
        ItemQuality.Masterwork,
      ),
      inventoryEquipment('fine', EquipmentType.Head, ItemQuality.Fine),
    ]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          { equipment: equipment.asReadonly() } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );
    component.enterScrapMode();

    component.setInventorySort({ main: 'Quality', sub: null });

    expect(component.filteredItems.map((item) => item.itemInstance.id)).toEqual(
      ['masterwork', 'fine', 'standard'],
    );
  });

  it('filters by an equipped slot and selects its current item for details', () => {
    const equippedHead = inventoryEquipment(
      'equipped-head',
      EquipmentType.Head,
    );
    const headOption = inventoryEquipment('head-option', EquipmentType.Head);
    const chestOption = inventoryEquipment('chest-option', EquipmentType.Chest);
    const equipment = signal<InventoryItem[]>([headOption, chestOption]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          { equipment: equipment.asReadonly() } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    component.selectEquipmentSlot({
      id: 'head-slot',
      iconPath: 'empty_head',
      equipmentSlotType: EquipmentSlotType.Head,
      equipmentInstance: equippedHead.itemInstance as EquipmentInstance,
    });

    expect(component.selectedEquipmentSlot()).toBe(EquipmentSlotType.Head);
    expect(component.selectedItem()?.itemInstance.id).toBe('equipped-head');
    expect(component.filteredItems.map((item) => item.itemInstance.id)).toEqual(
      ['head-option'],
    );
  });

  it('calculates gear power changes against the selected equipped item', () => {
    const equippedHead = inventoryEquipment(
      'equipped-head',
      EquipmentType.Head,
      ItemQuality.Standard,
      80,
    );
    const upgrade = inventoryEquipment(
      'upgrade',
      EquipmentType.Head,
      ItemQuality.Fine,
      95.25,
    );
    const equipment = signal<InventoryItem[]>([upgrade]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          { equipment: equipment.asReadonly() } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    component.selectEquipmentSlot({
      id: 'head-slot',
      iconPath: 'empty_head',
      equipmentSlotType: EquipmentSlotType.Head,
      equipmentInstance: equippedHead.itemInstance as EquipmentInstance,
    });

    expect(component.gearPowerDifference(upgrade)).toBe(15.25);
    expect(component.gearPowerDifference(equippedHead)).toBeNull();
  });

  it('sorts equipment by gear power from highest to lowest by default', () => {
    const equipment = signal<InventoryItem[]>([
      inventoryEquipment(
        'low-power',
        EquipmentType.Head,
        ItemQuality.Standard,
        12,
      ),
      inventoryEquipment(
        'high-power',
        EquipmentType.Head,
        ItemQuality.Standard,
        48,
      ),
    ]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          { equipment: equipment.asReadonly() } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    expect(component.inventorySort).toBe('Gear Power');
    expect(component.filteredItems.map((item) => item.itemInstance.id)).toEqual(
      ['high-power', 'low-power'],
    );
  });

  it('sorts stock alphabetically by default', () => {
    const materials = signal<InventoryItem[]>([
      inventoryStock('zinc', 'Zinc Ore'),
      inventoryStock('amber', 'Amber Shard'),
    ]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          {
            materials: materials.asReadonly(),
            essences: signal<InventoryItem[]>([]).asReadonly(),
          } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    expect(component.stockSort).toBe('Name');
    expect(
      component.filteredStockItems.map((item) => item.itemInstance.id),
    ).toEqual(['amber', 'zinc']);
  });

  it('separates entrance keys and catalysts from regular resources', () => {
    const materials = signal<InventoryItem[]>([
      inventoryStock('ore-stock', 'Iron Ore', 'ore'),
      inventoryStock(
        'goblin-sigil-stock',
        'Goblin Sigil',
        'sigil_goblin_mines',
      ),
      inventoryStock(
        'flame-catalyst-stock',
        'Flame Evolution Catalyst',
        'item.evolution_catalyst.flame',
      ),
      inventoryStock('warden-sigil-stock', 'Warden Sigil', 'warden_sigil'),
    ]);
    const objective = signal<QuestObjectiveState | undefined>(undefined);
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          {
            materials: materials.asReadonly(),
            essences: signal<InventoryItem[]>([]).asReadonly(),
          } as InventoryStateService,
          { pinnedObjective: objective.asReadonly() } as QuestStateService,
          jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
            'presentCurrentObjective',
          ]),
        ),
    );

    expect(
      component.filteredStockItems.map((item) => item.itemInstance.id),
    ).toEqual(['ore-stock']);

    component.selectStockCategory('Entrance Keys');
    expect(
      component.filteredStockItems.map((item) => item.itemInstance.id),
    ).toEqual(['goblin-sigil-stock']);

    component.selectStockCategory('Catalysts');
    expect(
      component.filteredStockItems.map((item) => item.itemInstance.id),
    ).toEqual(['flame-catalyst-stock', 'warden-sigil-stock']);
  });

  it('equips regular equipment directly into its natural slot', () => {
    const item = inventoryEquipment('head-option', EquipmentType.Head);
    const equipmentState = equipmentStateStub();
    const component = createComponentWithEquipmentState(equipmentState);

    component.equipItem(item);

    expect(equipmentState.equip).toHaveBeenCalledOnceWith(
      item.itemInstance as EquipmentInstance,
      EquipmentSlotType.Head,
    );
  });

  it('offers both hands for one-handed equipment and equips the chosen slot', () => {
    const item = inventoryEquipment('shortsword', EquipmentType.OneHanded);
    const equipmentState = equipmentStateStub();
    const component = createComponentWithEquipmentState(equipmentState);

    expect(component.equipSlotOptions(item)).toEqual([
      EquipmentSlotType.MainHand,
      EquipmentSlotType.OffHand,
    ]);

    component.equipItem(item, EquipmentSlotType.OffHand);

    expect(equipmentState.equip).toHaveBeenCalledOnceWith(
      item.itemInstance as EquipmentInstance,
      EquipmentSlotType.OffHand,
    );
  });
});

function createComponentWithEquipmentState(
  equipmentState: EquipmentStateService,
): InventoryComponent {
  return TestBed.runInInjectionContext(
    () =>
      new InventoryComponent(
        {} as InventoryStateService,
        {
          pinnedObjective: signal<QuestObjectiveState | undefined>(
            undefined,
          ).asReadonly(),
        } as QuestStateService,
        jasmine.createSpyObj<QuestPresenterService>('QuestPresenterService', [
          'presentCurrentObjective',
        ]),
        undefined,
        equipmentState,
      ),
  );
}

function equipmentStateStub(): EquipmentStateService & {
  equip: jasmine.Spy;
} {
  return {
    equipmentSlots: signal([]).asReadonly(),
    loading: signal(false).asReadonly(),
    equip: jasmine.createSpy('equip'),
    getSlot: jasmine.createSpy('getSlot'),
  } as unknown as EquipmentStateService & { equip: jasmine.Spy };
}

function inventoryStock(
  id: string,
  name: string,
  itemBaseId = `${id}-base`,
): InventoryItem {
  return {
    id,
    itemInstance: {
      id,
      itemBase: {
        id: itemBaseId,
        name,
        itemType: ItemType.Resource,
        rarity: Rarity.Common,
      },
    },
    quantity: 1,
  } as InventoryItem;
}

function inventoryEquipment(
  id: string,
  equipmentType: EquipmentType,
  quality = ItemQuality.Standard,
  itemBudget = 0,
): InventoryItem {
  return {
    id,
    itemInstance: {
      id,
      displayName: id,
      rarity: Rarity.Common,
      quality,
      tier: 1,
      itemBudget,
      itemBase: {
        id: `${id}-base`,
        name: id,
        itemType: ItemType.Equipment,
        rarity: Rarity.Common,
      },
      equipmentBase: { equipmentType },
    },
    quantity: 1,
  } as unknown as InventoryItem;
}
