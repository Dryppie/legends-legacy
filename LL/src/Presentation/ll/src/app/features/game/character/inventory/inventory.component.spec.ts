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
});

function inventoryEquipment(
  id: string,
  equipmentType: EquipmentType,
  quality = ItemQuality.Standard,
): InventoryItem {
  return {
    id,
    itemInstance: {
      id,
      displayName: id,
      rarity: Rarity.Common,
      quality,
      tier: 1,
      itemBudget: 0,
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
