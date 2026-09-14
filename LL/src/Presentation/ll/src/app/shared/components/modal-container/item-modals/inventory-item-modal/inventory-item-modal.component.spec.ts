import { signal } from '@angular/core';
import { QuestStateService } from '../../../../../core/services/api/quest/quest-state.service';
import { of } from 'rxjs';
import { InventoryService } from '../../../../../core/services/api/inventory/inventory.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import { ItemType } from '../../../../models/enums/itemType';
import { Rarity } from '../../../../models/enums/rarity';
import { InventoryItemModalComponent } from './inventory-item-modal.component';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';

describe('InventoryItemModalComponent selection containers', () => {
  let component: InventoryItemModalComponent;
  let inventoryService: jasmine.SpyObj<InventoryService>;
  let inventoryState: jasmine.SpyObj<InventoryStateService>;
  let essenceState: jasmine.SpyObj<EssenceStateService>;

  beforeEach(() => {
    inventoryService = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['openSelectionContainer'],
    );
    inventoryState = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['applyVersionedInventory'],
    );
    essenceState = jasmine.createSpyObj<EssenceStateService>(
      'EssenceStateService',
      ['refreshArchive'],
      {
        absorbedEssenceDefinitionIds: signal(new Set(['skeleton'])),
      },
    );
    component = new InventoryItemModalComponent(
      inventoryService,
      inventoryState,
      essenceState,
    );
    component.inventoryItem = {
      id: 'token',
      quantity: 1,
      itemInstance: {
        id: 'token-instance',
        itemBase: {
          id: 'item.essence_token.lumo_ruins',
          name: 'Lumo Ruins - Essence Token',
          description: '',
          stackable: true,
          itemType: ItemType.Resource,
          rarity: Rarity.Rare,
          selectionCrate: {
            selectionLabel: 'Essence',
            options: [
              { id: 'goblin', name: 'Goblin Essence', quantity: 1 },
              { id: 'skeleton', name: 'Skeleton Essence', quantity: 1 },
            ],
          },
        },
      },
    } as InventoryItem;
  });

  it('starts without a selected Essence and does not redeem until one is chosen', () => {
    component.ngOnInit();

    expect(component.selectedCrateOptionId()).toBe('');
    expect(essenceState.refreshArchive).toHaveBeenCalledOnceWith();
    expect(
      component.isCrateOptionAbsorbed(component.selectionCrate!.options[0]),
    ).toBeFalse();
    expect(
      component.isCrateOptionAbsorbed(component.selectionCrate!.options[1]),
    ).toBeTrue();
    component.openSelectionCrate();
    expect(inventoryService.openSelectionContainer).not.toHaveBeenCalled();
    expect(component.isOpeningCrate()).toBeFalse();

    const response = {
      data: {
        consumedItemInstanceId: 'token-instance',
        grantId: 'grant-1',
        rewards: [],
        inventoryItems: [],
      },
      domainVersions: { inventory: 1 },
    };
    inventoryService.openSelectionContainer.and.returnValue(of(response));
    spyOn(component.close, 'emit');
    component.selectCrateOption(component.selectionCrate!.options[1]);
    component.openSelectionCrate();

    expect(inventoryService.openSelectionContainer).toHaveBeenCalledOnceWith(
      'token-instance',
      'skeleton',
    );
    expect(inventoryState.applyVersionedInventory).toHaveBeenCalledOnceWith(
      response,
      'grant-1',
    );
    expect(component.close.emit).toHaveBeenCalledOnceWith();
  });

  it('preserves the default selection for other containers', () => {
    component.inventoryItem.itemInstance.itemBase.id =
      'item.catalyst_selection_crate';
    component.inventoryItem.itemInstance.itemBase.selectionCrate = {
      selectionLabel: 'Catalyst',
      options: [{ id: 'flame', name: 'Flame Evolution Catalyst', quantity: 6 }],
    };

    component.ngOnInit();

    expect(component.selectedCrateOptionId()).toBe('flame');
    expect(essenceState.refreshArchive).not.toHaveBeenCalled();
  });

  it('opens random equipment boxes without choosing an item', () => {
    component.inventoryItem.itemInstance.itemBase.id =
      'item.uncommon_equipment_box';
    component.inventoryItem.itemInstance.itemBase.selectionCrate = {
      selectionLabel: 'Equipment',
      isRandom: true,
      options: [],
    };
    inventoryService.openSelectionContainer.and.returnValue(
      of({
        data: {
          consumedItemInstanceId: 'token-instance',
          grantId: 'box-grant',
          rewards: [],
          inventoryItems: [],
        },
        domainVersions: { inventory: 1 },
      }),
    );

    component.ngOnInit();
    component.openSelectionCrate();

    expect(inventoryService.openSelectionContainer).toHaveBeenCalledOnceWith(
      'token-instance',
      'random',
    );
    expect(inventoryState.applyVersionedInventory).toHaveBeenCalled();
  });
});
