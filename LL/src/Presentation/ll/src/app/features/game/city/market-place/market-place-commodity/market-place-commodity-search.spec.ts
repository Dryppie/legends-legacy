import { ItemType } from '../../../../../shared/models/enums/itemType';
import { EssenceItem } from '../../../../../shared/models/item';
import { marketplaceCommoditySearchText } from './market-place-commodity-search';

describe('marketplaceCommoditySearchText', () => {
  it('includes every Essence and ability tag source', () => {
    const essenceItem = {
      name: 'Unbound Thornback Boar Essence',
      description: 'An unbound Essence.',
      itemType: ItemType.Essence,
      essence: {
        name: 'Thornback Boar Essence',
        variantName: 'Thornback',
        displayName: 'Thornback Boar',
        description: 'A defensive Essence.',
        tagsByCategory: {
          Role: ['Role.Defensive'],
          Mechanic: ['Mechanic.Retaliation'],
        },
        activeAbility: {
          name: 'Thorned Rush',
          tags: ['Physical'],
        },
        passiveAbility: {
          name: 'Bristling Hide',
          tags: ['Buff'],
        },
        evolution: {
          addsTags: ['Mechanic.Execute'],
        },
      },
    } as unknown as EssenceItem;

    const searchable = marketplaceCommoditySearchText(essenceItem);

    expect(searchable).toContain('role.defensive');
    expect(searchable).toContain('mechanic.retaliation');
    expect(searchable).toContain('thorned rush');
    expect(searchable).toContain('physical');
    expect(searchable).toContain('bristling hide');
    expect(searchable).toContain('buff');
    expect(searchable).toContain('mechanic.execute');
  });
});
