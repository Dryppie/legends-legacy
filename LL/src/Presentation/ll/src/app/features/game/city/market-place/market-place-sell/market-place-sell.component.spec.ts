import { provideFreeNobilityForTests } from '../../../../../core/services/api/nobility/nobility.testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { MarketPlaceSellComponent } from './market-place-sell.component';

describe('MarketPlaceSellComponent Blueprints', () => {
  it('lets an alpha player select a Signet stack for sale', () => {
    TestBed.configureTestingModule({ providers: provideFreeNobilityForTests() });
    const signet = resource('signet');
    signet.itemInstance.itemBase.itemType = ItemType.Misc;
    const component = TestBed.runInInjectionContext(() => new MarketPlaceSellComponent(
      { items: signal([signet, resource('ore')]), load: () => undefined } as unknown as InventoryStateService,
      { load: () => undefined, myListings: signal([]), myBuyOrders: signal([]), buyOrders: signal([]) } as unknown as MarketplaceStateService,
    ));
    component.itemType = ItemType.Misc;
    component.category = 'signets';
    expect(component.inventoryTitle).toBe('Signets');
    expect(component.filteredItems).toEqual([signet]);
    component.selectItem(signet);
    expect(component.pendingItem()).toEqual(signet);
  });
  it('lists an unbound Blueprint stack and excludes bound Blueprints and other resources', () => {
    TestBed.configureTestingModule({ providers: provideFreeNobilityForTests() });
    const blueprint = resource('item.blueprint_fury');
    const bound = resource('item.blueprint_arcane', true);
    const items = [blueprint, bound, resource('item.monster_core.lesser'), resource('ore')];
    const createListing = jasmine.createSpy('createListing').and.returnValue(of({}));
    const component = TestBed.runInInjectionContext(() => new MarketPlaceSellComponent(
      {
        materials: signal(items),
        items: signal(items),
        load: () => undefined,
      } as unknown as InventoryStateService,
      {
        load: () => undefined,
        myListings: signal([]),
        myBuyOrders: signal([]),
        buyOrders: signal([]),
        createListing,
      } as unknown as MarketplaceStateService,
    ));
    component.itemType = ItemType.Resource;
    component.category = 'blueprints';
    component.subcategory = 'Blueprints';

    expect(component.inventoryTitle).toBe('Blueprints');
    expect(component.filteredItems).toEqual([blueprint]);
    component.selectItem(bound);
    expect(component.pendingItem()).toBeNull();
    component.selectItem(blueprint);
    TestBed.flushEffects();
    component.qtyCtrl.setValue(2);
    component.priceCtrl.setValue(100);
    expect(component.canCreateListing()).toBeTrue();

    component.listItem();
    expect(createListing).toHaveBeenCalledOnceWith(blueprint, 2, 100);
    expect(component.pendingItem()).toBeNull();
  });
});

function resource(id: string, isBound = false): InventoryItem {
  return {
    id: `inventory-${id}`,
    quantity: 3,
    itemInstance: {
      id: `instance-${id}`,
      itemBase: { id, name: id, itemType: ItemType.Resource, stackable: true, isBound },
    },
  } as InventoryItem;
}
