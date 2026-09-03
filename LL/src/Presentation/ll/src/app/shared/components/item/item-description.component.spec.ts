import { ItemBase } from '../../models/item';
import { itemDescription } from '../../utils/inventory/item-description';
describe('Current item descriptions', () => {
  it('keeps authored descriptions for other items', () => {
    expect(itemDescription({ id: 'soul_dust', description: 'An Essence resource.' } as ItemBase)).toBe('An Essence resource.');
  });
});
