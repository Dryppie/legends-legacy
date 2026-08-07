import { AbilityTagsComponent } from './ability-tags.component';

describe('AbilityTagsComponent', () => {
  it('deduplicates tags while preserving their authored order', () => {
    const component = new AbilityTagsComponent();

    component.tags = ['Magical', 'Ranged', 'Poison', 'Debuff', 'magical'];

    expect(component.displayTags).toEqual([
      'Magical',
      'Ranged',
      'Poison',
      'Debuff',
    ]);
  });
});
