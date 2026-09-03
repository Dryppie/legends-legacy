import { signal } from '@angular/core';
import { QuestStateService } from '../../../../../core/services/api/quest/quest-state.service';
import { of } from 'rxjs';
import { InventoryService } from '../../../../../core/services/api/inventory/inventory.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import { ItemType } from '../../../../models/enums/itemType';
import { Rarity } from '../../../../models/enums/rarity';
import { InventoryItemModalComponent } from './inventory-item-modal.component';

describe('InventoryItemModalComponent selection containers', () => {
  let component: InventoryItemModalComponent;
  let inventoryService: jasmine.SpyObj<InventoryService>;
  let inventoryState: jasmine.SpyObj<InventoryStateService>;

  beforeEach(() => {
    inventoryService = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['openSelectionContainer'],
    );
    inventoryState = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['applyVersionedInventory'],
    );
    component = new InventoryItemModalComponent(inventoryService,
inventoryState
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
  });
});
