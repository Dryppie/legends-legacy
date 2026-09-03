import { ItemBase } from '../../models/item';
import { itemDescription } from '../../utils/inventory/item-description';
describe('Current item descriptions', () => {
  it('explains reusable Blueprint styles and their container', () => {
    expect(itemDescription({ id: 'blueprint_fury' } as ItemBase)).toContain('reusable equipment style');
    expect(itemDescription({ id: 'item.blueprint_selection_box' } as ItemBase)).toContain('Choose a Blueprint');
  });
  it('keeps authored descriptions for other items', () => {
    expect(itemDescription({ id: 'soul_dust', description: 'An Essence resource.' } as ItemBase)).toBe('An Essence resource.');
  });
});
