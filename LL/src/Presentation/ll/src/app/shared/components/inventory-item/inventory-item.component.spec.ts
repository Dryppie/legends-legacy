import { ItemType } from '../../models/enums/itemType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { InventoryItem } from '../../models/inventoryItem';
import { EquipmentInstance } from '../../models/item';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { InventoryItemComponent } from './inventory-item.component';

describe('InventoryItemComponent', () => {
  const modal = jasmine.createSpyObj<ModalService>('ModalService', [
    'toggleInventoryEquipItemModal',
    'toggleInventoryItemModal',
  ]);

  it('exposes equipment quality and gear value for inventory summaries', () => {
    const equipment = {
      id: 'equipment-1',
      itemBase: { itemType: ItemType.Equipment },
      quality: ItemQuality.Fine,
      itemBudget: 42.5,
    } as EquipmentInstance;
    const component = new InventoryItemComponent(modal);
    component.inventoryItem = {
      id: 'inventory-1',
      itemInstance: equipment,
      quantity: 1,
    } as InventoryItem;
    component.showEquipmentSummary = true;

    expect(component.equipment?.quality).toBe(ItemQuality.Fine);
    expect(component.equipment?.itemBudget).toBe(42.5);
    expect(component.showEquipmentSummary).toBeTrue();
  });

  it('does not expose an equipment summary for regular items', () => {
    const component = new InventoryItemComponent(modal);
    component.inventoryItem = {
      id: 'inventory-2',
      itemInstance: {
        id: 'resource-1',
        itemBase: { itemType: ItemType.Resource },
      },
      quantity: 3,
    } as InventoryItem;

    expect(component.equipment).toBeNull();
    expect(component.showEquipmentSummary).toBeFalse();
  });
});
