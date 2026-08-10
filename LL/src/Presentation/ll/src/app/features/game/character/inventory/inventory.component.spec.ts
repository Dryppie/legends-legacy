import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { QuestPresenterService } from '../../../../core/services/api/quest/quest-presenter.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
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
          jasmine.createSpyObj<QuestPresenterService>(
            'QuestPresenterService',
            ['presentCurrentObjective'],
          ),
        ),
    );

    expect(
      component.scrapableEquipment().map((item) => item.itemInstance.id),
    ).toEqual(['weapon']);
  });
});

function inventoryEquipment(
  id: string,
  equipmentType: EquipmentType,
): InventoryItem {
  return {
    id,
    itemInstance: {
      id,
      equipmentBase: { equipmentType },
    },
    quantity: 1,
  } as unknown as InventoryItem;
}
