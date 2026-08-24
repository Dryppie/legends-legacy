import { BlueprintItemMetadata } from '../../models/item';
import { getBlueprintContributedAttributes } from './blueprint-attribute-summary.component';

describe('getBlueprintContributedAttributes', () => {
  it('returns positive blueprint attributes in contribution order', () => {
    const blueprint: BlueprintItemMetadata = {
      blueprintId: 'blueprint_fury',
      name: 'Blueprint: Fury',
      bonusStatProfile: {
        CritDamage: 0.25,
        Power: 0.45,
        CritChance: 0.3,
        Armor: 0,
      },
      requiredRecipeTags: [],
      anyRecipeTags: [],
      compatibleRecipeCount: 0,
      compatibleRecipes: [],
    };

    expect(getBlueprintContributedAttributes(blueprint)).toEqual([
      'Power',
      'CritChance',
      'CritDamage',
    ]);
  });
});
