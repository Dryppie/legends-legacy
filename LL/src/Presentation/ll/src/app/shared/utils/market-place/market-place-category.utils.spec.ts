import { ItemType } from '../../models/enums/itemType';
import { ItemBase } from '../../models/item';
import {
  getMarketplaceResourceSortRank,
  isMarketplaceTradableItemBase,
  matchesMarketplaceResourceSubcategory,
} from './market-place-category.utils';

describe('marketplace category matching', () => {
  it('limits Monster Cores to the three core tiers, excluding legacy materials and catalysts', () => {
    for (const tier of ['lesser', 'greater', 'primal']) {
      expect(
        matchesMarketplaceResourceSubcategory(
          resource(`item.monster_core.${tier}`),
          'Monster Cores',
        ),
      ).toBeTrue();
    }

    for (const id of [
      'ore',
      'bloodwood',
      'venom_gland',
      'item.evolution_catalyst.cunning',
    ]) {
      expect(
        matchesMarketplaceResourceSubcategory(resource(id), 'Monster Cores'),
      ).toBeFalse();
    }
  });

  it('sorts Monster Cores from lesser through primal', () => {
    expect(
      ['lesser', 'greater', 'primal'].map((tier) =>
        getMarketplaceResourceSortRank(
          resource(`item.monster_core.${tier}`),
          'Monster Cores',
        ),
      ),
    ).toEqual([0, 1, 2]);
  });

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

  it('keeps catalysts out of normal resource families', () => {
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('venom_gland', 'Venom Catalyst'),
        'Ore',
      ),
    ).toBeFalse();
  });

  it('allows unbound items and rejects bound items from marketplace actions', () => {
    const unbound = resource('ore');
    const bound = { ...resource('sigil_goblin_mines'), isBound: true };

    expect(isMarketplaceTradableItemBase(unbound)).toBeTrue();
    expect(isMarketplaceTradableItemBase(bound)).toBeFalse();
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
