import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER } from 'rxjs';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { MarketPlaceService } from '../../../../../core/services/api/market-place/market-place.service';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { EssenceItem, ItemBase } from '../../../../../shared/models/item';
import { MarketPlaceCommodityComponent } from './market-place-commodity.component';

describe('MarketPlaceCommodityComponent absorbed Essences', () => {
  const absorbed = essenceBase('venomous_snake');
  const unabsorbed = essenceBase('viper');
  const archive = signal<unknown>({ essences: [], essenceDust: 0 });
  const refreshArchive = jasmine.createSpy('refreshArchive');

  function createComponent(
    catalog: ItemBase[] = [absorbed, unabsorbed],
    absorbedIds: string[] = ['venomous_snake'],
  ): MarketPlaceCommodityComponent {
    refreshArchive.calls.reset();

    const component = TestBed.runInInjectionContext(
      () =>
        new MarketPlaceCommodityComponent(
          {
            items: signal([]),
            load: jasmine.createSpy('load'),
          } as unknown as InventoryStateService,
          {
            catalog: signal(catalog),
            listings: signal([]),
            buyOrders: signal([]),
            load: jasmine.createSpy('load'),
          } as unknown as MarketplaceStateService,
          {
            currentCharacterId: signal('current-character'),
          } as unknown as CharacterService,
          {
            getSummary: jasmine.createSpy('getSummary').and.returnValue(NEVER),
          } as unknown as MarketPlaceService,
          {
            archive,
            absorbedEssenceDefinitionIds: signal(new Set(absorbedIds)),
            refreshArchive,
          } as unknown as EssenceStateService,
        ),
    );

    component.itemType = ItemType.Essence;
    component.category = 'essences';
    return component;
  }

  beforeEach(() => {
    archive.set({ essences: [], essenceDust: 0 });
    TestBed.configureTestingModule({});
  });

  it('marks catalogue entries that are already in the Soul Archive', () => {
    const component = createComponent();

    expect(component.isAbsorbedEssence(absorbed)).toBeTrue();
    expect(component.isAbsorbedEssence(unabsorbed)).toBeFalse();
  });

  it('never marks non-Essence catalogue entries', () => {
    const component = createComponent();
    const resource: ItemBase = {
      id: 'iron_ore',
      name: 'Iron Ore',
      description: '',
      itemType: ItemType.Resource,
      stackable: true,
    } as unknown as ItemBase;

    expect(component.isAbsorbedEssence(resource)).toBeFalse();
  });

  it('filters the catalogue down to Essences that are not absorbed yet', () => {
    const component = createComponent();

    expect(
      component.filteredCommodities().map((commodity) => commodity.base.id),
    ).toEqual(['item.venomous_snake', 'item.viper']);

    component.setCatalogStatus('unabsorbed');

    expect(
      component.filteredCommodities().map((commodity) => commodity.base.id),
    ).toEqual(['item.viper']);
  });

  it('drops the Essence-only filter when leaving the Essence catalogue', () => {
    const component = createComponent();
    component.setCatalogStatus('unabsorbed');

    component.category = 'resources';

    expect(component.catalogStatus()).toBe('all');
  });

  it('loads the Soul Archive once when it has not been fetched yet', () => {
    archive.set(null);
    createComponent();
    TestBed.flushEffects();

    expect(refreshArchive).toHaveBeenCalledTimes(1);
  });

  it('does not refetch the Soul Archive when it is already loaded', () => {
    createComponent();
    TestBed.flushEffects();

    expect(refreshArchive).not.toHaveBeenCalled();
  });
});

function essenceBase(definitionId: string): EssenceItem {
  return {
    id: `item.${definitionId}`,
    name: `Unbound ${definitionId} Essence`,
    description: 'A tradable Essence.',
    itemType: ItemType.Essence,
    stackable: true,
    essenceDefinitionId: definitionId,
    dismantleDustAmount: 1,
  } as unknown as EssenceItem;
}
