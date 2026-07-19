import { ItemType } from '../../models/enums/itemType';
import { ItemBase } from '../../models/item';
import {
  isMarketplaceBlueprintResource,
  matchesMarketplaceResourceSubcategory,
} from './market-place-category.utils';

describe('marketplace category matching', () => {
  it('matches only resources from the selected family', () => {
    expect(
      matchesMarketplaceResourceSubcategory(resource('ore'), 'Metal'),
    ).toBeTrue();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('bone_fragments'),
        'Metal',
      ),
    ).toBeFalse();
  });

  it('keeps blueprints and catalysts out of normal resource families', () => {
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('blueprint_aegis', 'Blueprint: Aegis'),
        'Metal',
      ),
    ).toBeFalse();
    expect(
      matchesMarketplaceResourceSubcategory(
        resource('venom_gland', 'Venom Gland'),
        'Metal',
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
