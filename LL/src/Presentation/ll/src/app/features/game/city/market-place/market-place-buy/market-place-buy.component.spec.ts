import { Component, Input, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { MarketPlaceBuyComponent } from './market-place-buy.component';
import { MarketPlaceSellComponent } from '../market-place-sell/market-place-sell.component';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { QuestStateService } from '../../../../../core/services/api/quest/quest-state.service';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { MarketPlaceListingItemComponent } from '../../../../../shared/components/market-place/market-place-listing-item/market-place-listing-item.component';
import { DropdownComponent } from '../../../../../shared/components/custom-components/dropdown/dropdown.component';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import {
  EquipmentInstance,
  ItemInstance,
} from '../../../../../shared/models/item';
import { EquipmentType } from '../../../../../shared/models/enums/equipmentType';
import { ItemType } from '../../../../../shared/models/enums/itemType';

@Component({
  selector: 'app-item',
  standalone: true,
  template: '{{ item.displayName }}',
})
class ItemStub {
  @Input() item!: ItemInstance;
}

describe('equipment marketplace', () => {
  const journal = signal({ quests: [{ requiresEquipmentProgression: true }] });
  const loaded = signal(true);
  const characterId = signal('buyer');
  const listings = signal<MarketPlaceListing[]>([]);
  const purchase = jasmine.createSpy('buyoutListing');
  let fixture: ComponentFixture<MarketPlaceBuyComponent>;
  let component: MarketPlaceBuyComponent;

  beforeEach(async () => {
    journal.set({ quests: [{ requiresEquipmentProgression: true }] });
    loaded.set(true);
    characterId.set('buyer');
    listings.set([
      listing('fury', 3, 'blueprint_fury'),
      listing('plain', 1, null),
      listing('second-fury', 1, 'blueprint_fury'),
      listing('legacy', null, null),
      listing('bound', 4, 'blueprint_fury', 'BoundPersonal'),
      listing('guild', 4, null, 'GuildOwned'),
    ]);
    purchase.calls.reset();
    purchase.and.returnValue(of({ remainingListing: null }));
    await TestBed.configureTestingModule({
      imports: [MarketPlaceBuyComponent],
      providers: [
        {
          provide: MarketplaceStateService,
          useValue: {
            listings,
            load: () => undefined,
            buyoutListing: purchase,
          },
        },
        {
          provide: InventoryStateService,
          useValue: { items: signal([]), load: () => undefined },
        },
        {
          provide: EquipmentStateService,
          useValue: { load: () => undefined, getSlot: () => null },
        },
        {
          provide: CharacterStateService,
          useValue: { currentCharacterId: characterId },
        },
        {
          provide: QuestStateService,
          useValue: { journal, loaded, load: () => undefined },
        },
      ],
    })
      .overrideComponent(MarketPlaceBuyComponent, {
        remove: { imports: [ItemComponent] },
        add: { imports: [ItemStub] },
      })
      .overrideComponent(MarketPlaceListingItemComponent, {
        remove: { imports: [ItemComponent] },
        add: { imports: [ItemStub] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(MarketPlaceBuyComponent);
    component = fixture.componentInstance;
    component.itemType = ItemType.Equipment;
    fixture.detectChanges();
  });

  function ids(): string[] {
    return component.filteredListings().map((item) => item.id);
  }
  function dropdown(label: string): DropdownComponent {
    return fixture.debugElement
      .queryAll(By.directive(DropdownComponent))
      .find((element) => element.componentInstance.label === label)!
      .componentInstance;
  }

  it('shows canonical controls and rows while excluding legacy and bound listings', () => {
    expect(ids()).toEqual(['fury', 'plain', 'second-fury']);
    expect(
      fixture.nativeElement.querySelector(
        '[aria-label="Minimum equipment rank"]',
      ),
    ).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector(
        '[aria-label="Minimum equipment potential"]',
      ),
    ).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Any quality');
    expect(fixture.nativeElement.textContent).toContain('Tier 1 · Rank 3');
    expect(fixture.nativeElement.textContent).toContain('Fury');
    expect(
      component.definitionOptions().map((option) => option.value),
    ).not.toContain('bound');
    expect(component.styleOptions().map((option) => option.label)).toEqual([
      'Any active style',
      'Plain',
      'Fury',
    ]);
  });

  it('combines identity, active style, rank, tier and slot filters and resets them', () => {
    component.quality.set('Impossible legacy quality');
    component.minimumPotentialCtrl.setValue(999);
    dropdown('Any active style').selection.emit({
      main: 'blueprint_fury',
      sub: null,
    });
    component.minimumRankCtrl.setValue(2);
    component.minimumTierCtrl.setValue(1);
    component.equipmentType.set(EquipmentType.OneHanded);
    fixture.detectChanges();
    expect(ids()).toEqual(['fury']);
    dropdown('Any identity').selection.emit({ main: 'plain', sub: null });
    fixture.detectChanges();
    expect(ids()).toEqual([]);
    component.resetFilters();
    fixture.detectChanges();
    expect(ids()).toEqual(['fury', 'plain', 'second-fury']);
    dropdown('Any active style').selection.emit({ main: 'plain', sub: null });
    fixture.detectChanges();
    expect(ids()).toEqual(['plain']);
    component.minimumTierCtrl.setValue(2);
    expect(ids()).toEqual([]);
  });

  it('searches canonical identities and styles and keeps price sorting', async () => {
    component.searchCtrl.setValue('fury');
    await new Promise((resolve) => setTimeout(resolve, 350));
    fixture.detectChanges();
    expect(ids()).toEqual(['fury', 'second-fury']);
    component.togglePriceSort();
    expect(ids()).toEqual(['second-fury', 'fury']);
    component.searchCtrl.setValue('plain.shortsword');
    await new Promise((resolve) => setTimeout(resolve, 350));
    fixture.detectChanges();
    expect(ids()).toEqual(['plain', 'second-fury', 'fury']);
  });

  it('previews binding and styles and purchases the exact selected instance once', () => {
    const item = listings()[0];
    component.selectListing(item);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Native style');
    expect(fixture.nativeElement.textContent).toContain('Active style');
    expect(fixture.nativeElement.textContent).toContain(
      'Buying keeps this item tradeable',
    );
    component.buyoutListing();
    expect(purchase).toHaveBeenCalledOnceWith(item.id, 1);
    expect(component.selectedListing()).toBeNull();
    component.buyoutListing();
    expect(purchase).toHaveBeenCalledTimes(1);
  });

  it('refreshes changed selections and clears removed listings or changed characters', () => {
    component.selectListing(listings()[0]);
    fixture.detectChanges();
    const changed = { ...listings()[0], unitPrice: 250 };
    listings.set([changed]);
    fixture.detectChanges();
    expect(component.selectedListing()?.unitPrice).toBe(250);
    listings.set([]);
    fixture.detectChanges();
    expect(component.selectedListing()).toBeNull();
    component.buyoutListing();
    expect(purchase).not.toHaveBeenCalled();
    listings.set([changed]);
    fixture.detectChanges();
    component.selectListing(changed);
    component.definitionId.set('fury');
    characterId.set('another-buyer');
    fixture.detectChanges();
    expect(component.selectedListing()).toBeNull();
    expect(component.definitionId()).toBe('');
  });

  it('excludes personal-bound and guild equipment from selling and shows ranks in own listings', () => {
    const items = listings().map(
      (l) =>
        ({
          id: 'inventory-' + l.id,
          itemInstance: l.itemInstance,
          quantity: 1,
        }) as InventoryItem,
    );
    const sell = TestBed.runInInjectionContext(
      () =>
        new MarketPlaceSellComponent(
          {
            equipment: signal(items),
            items: signal(items),
            load: () => undefined,
          } as unknown as InventoryStateService,
          {
            load: () => undefined,
            myListings: signal([]),
            myBuyOrders: signal([]),
            buyOrders: signal([]),
          } as unknown as MarketplaceStateService,
        ),
    );
    sell.itemType = ItemType.Equipment;
    sell.category = 'equipment';
    expect(sell.filteredItems.map((item) => item.itemInstance.id)).toEqual([
      'item-fury',
      'item-plain',
      'item-second-fury',
      'item-legacy',
    ]);
    sell.selectItem(items[4]);
    expect(sell.pendingItem()).toBeNull();
    sell.pendingItem.set(items[5]);
    sell.priceCtrl.setValue(100);
    expect(sell.canCreateListing()).toBeFalse();
    expect(sell.listingEquipmentSummary(listings()[0])).toBe('Tier 1 · Rank 3');
    expect(sell.listingEquipmentSummary(listings()[3])).toBe('Fine');
  });

  it('prevents own, unavailable or hidden purchases', () => {
    const own = { ...listings()[0], sellerId: 'buyer' };
    listings.set([own]);
    fixture.detectChanges();
    component.selectListing(own);
    component.buyoutListing();
    expect(purchase).not.toHaveBeenCalled();
    component.selectListing(listing('hidden', null, null));
    expect(component.selectedListing()?.id).toBe(own.id);
  });
});

function listing(
  id: string,
  rank: number | null,
  style: string | null,
  ownership = 'UnboundPersonal',
): MarketPlaceListing {
  const base = {
    id: 'shortsword',
    name: 'Shortsword',
    itemType: ItemType.Equipment,
    equipmentType: EquipmentType.OneHanded,
    stackable: false,
    isBound: false,
  };
  const item = {
    id: 'item-' + id,
    displayName: id + ' sword',
    itemBase: base,
    equipmentBase: base,
    tier: 1,
    rarity: 'Rare',
    quality: 'Fine',
    potential: 40,
    attributeModifiers: [],
    baseModifiers: [],
    instanceModifiers: [],
    progression:
      rank === null
        ? null
        : {
            definitionId: id,
            archetypeId: 'plain.shortsword',
            rank,
            nativeStyleId: style,
            activeStyleId: style,
            ownership,
            modelVersion: 1,
            balanceVersion: 1,
            paidScrap: 0,
            paidCinders: 0,
          },
  } as unknown as EquipmentInstance;
  return {
    id,
    itemInstance: item,
    itemInstanceId: item.id,
    sellerId: 'seller',
    sellerName: 'Seller',
    createdAt: new Date(),
    expiresAt: new Date(Date.now() + 86400000),
    quantity: 1,
    unitPrice: id === 'fury' ? 100 : 50,
  };
}
