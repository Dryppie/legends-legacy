import { craftingTabIndex } from './crafting.component';

describe('craftingTabIndex', () => {
  it('opens tempering when requested by current-action navigation', () => {
    expect(craftingTabIndex('tempering')).toBe(1);
    expect(craftingTabIndex('Tempering')).toBe(1);
  });

  it('defaults to the regular crafting tab', () => {
    expect(craftingTabIndex(null)).toBe(0);
    expect(craftingTabIndex('crafting')).toBe(0);
  });
});
