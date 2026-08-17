import { ItemType } from '../../models/enums/itemType';
import { ItemBase } from '../../models/item';
import {
  getMarketplaceResourceSortRank,
  isMarketplaceBlueprintResource,
  matchesMarketplaceResourceSubcategory,
} from './market-place-category.utils';

describe('marketplace category matching', () => {
  it('matches only resources from the selected family', () => {
    expect(
      matchesMarketplaceResourceSubcategory(resource('ore'), 'Ore'),
    ).toBeTrue();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('rawhide'),
        'Ore',
      ),
    ).toBeFalse();
  });

  it('includes tier two materials in their resource families', () => {
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('copper_ore', 'Copper Ore'),
        'Ore',
      ),
    ).toBeTrue();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('bloodwood', 'Bloodwood'),
        'Wood',
      ),
    ).toBeTrue();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('thick_hide', 'Thick Hide'),
        'Hide',
      ),
    ).toBeTrue();
  });

  it('sorts tier one before tier two within each resource family', () => {
    expect(getMarketplaceResourceSortRank(resource('ore'), 'Ore')).toBe(0);
    expect(
      getMarketplaceResourceSortRank(resource('copper_ore'), 'Ore'),
    ).toBe(1);
  });

  it('keeps blueprints and catalysts out of normal resource families', () => {
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('blueprint_aegis', 'Blueprint: Aegis'),
        'Ore',
      ),
    ).toBeFalse();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('venom_gland', 'Venom Gland'),
        'Ore',
      ),
    ).toBeFalse();
  });

  it('recognizes blueprint resources consistently', () => {
    expect(
      isMarketplaceBlueprintResource(
        resource('blueprint_aegis', 'Blueprint: Aegis'),
      ),
    ).toBeTrue();
  });
});

function resource(id: string, name = id): ItemBase {
  return {
    id,
    name,
    itemType: ItemType.Resource,
    stackable: true,
  } as ItemBase;
}
