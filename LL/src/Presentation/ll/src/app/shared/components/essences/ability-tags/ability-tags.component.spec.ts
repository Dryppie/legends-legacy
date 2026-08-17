import { AbilityTagsComponent } from './ability-tags.component';
import { ABILITY_TARGETS } from '../ability-target-glossary';
import { ABILITY_TARGET_SELECTORS } from '../../../models/enums/targeting';

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

  it('has player-facing copy for every target selector', () => {
    expect(ABILITY_TARGETS.map((target) => target.selector)).toEqual([
      ...ABILITY_TARGET_SELECTORS,
    ]);
    expect(
      ABILITY_TARGETS.every(
        (target) => target.label.length > 0 && target.description.length > 0,
      ),
    ).toBeTrue();
  });

  it('deduplicates targets while preserving their effect order', () => {
    const component = new AbilityTagsComponent();

    component.targets = ['CurrentTarget', 'Self', 'CurrentTarget'];

    expect(component.displayTargets.map((target) => target.selector)).toEqual([
      'CurrentTarget',
      'Self',
    ]);
  });

  it('explains the inclusion and selection rules for ambiguous targets', () => {
    const component = new AbilityTagsComponent();
    component.targets = ['AllAllies', 'CurrentTarget'];

    expect(component.displayTargets[0].description).toContain(
      'including the user and allied summons',
    );
    expect(component.displayTargets[1].description).toContain(
      'selected using threat',
    );
    expect(component.displayTargets[1].description).toContain(
      'lock this enemy',
    );
  });
});
