import { EssenceEffectDto } from '../../../models/essence-system';
import { EssenceDescriptionFormatter } from './essence-description-formatter';

describe('EssenceDescriptionFormatter', () => {
  const formatter = new EssenceDescriptionFormatter();

  it('keeps authored damage text and adds a character-scaled damage preview', () => {
    const root = render(
      formatter.format(
        'Deal 100% Physical Damage to a random enemy.',
        [effect('Damage', 'Power', 1)],
        (attribute) => (attribute === 'Power' ? 100 : 0),
      ),
    );
    const magnitude = root.querySelector<HTMLElement>('.dmg');

    expect(root.textContent).toBe(
      'Deal 100% Physical Damage to a random enemy.',
    );
    expect(magnitude?.textContent).toBe('100% Physical Damage');
    expect(magnitude?.dataset['display']).toBe('80-120');
    expect(magnitude?.dataset['scaleDisplay']).toBe('100%');
    expect(magnitude?.dataset['attr']).toBe('Power');
  });

  it('previews healing from the current scaling attribute', () => {
    const root = render(
      formatter.format(
        'Heal the lowest-health ally for 35% Power.',
        [effect('Heal', 'Power', 0.35)],
        (attribute) => (attribute === 'Power' ? 200 : 0),
      ),
    );
    const magnitude = root.querySelector<HTMLElement>('.heal');

    expect(magnitude?.textContent).toBe('35% Power');
    expect(magnitude?.dataset['display']).toBe('56-84');
    expect(magnitude?.dataset['unit']).toBe('healing');
  });

  it('adds definitions and authored values to standard combat keywords', () => {
    const root = render(
      formatter.format('Apply Poison(18), then apply Slow.', [], () => 0),
    );
    const keywords = Array.from(root.querySelectorAll<HTMLElement>('.keyword'));

    expect(keywords.map((keyword) => keyword.textContent)).toEqual([
      'Poison(18)',
      'Slow',
    ]);
    expect(keywords[0].dataset['title']).toBe('Poison');
    expect(keywords[0].dataset['description']).toBe(
      'Poison(18) deals 18% of your Power every 2 seconds for 12 seconds.',
    );
    expect(keywords[0].dataset['detail']).toBe('');
    expect(keywords[1].dataset['description']).toContain(
      'Reduces Basic Attack rate by 25%',
    );
  });

  it('uses fluid, value-aware descriptions for damage-over-time keywords', () => {
    const root = render(
      formatter.format('Apply Burn(12) and Bleed(20).', [], () => 0),
    );
    const keywords = Array.from(root.querySelectorAll<HTMLElement>('.keyword'));

    expect(keywords[0].dataset['description']).toBe(
      'Burn(12) deals 12% of your Power every second for 4 seconds.',
    );
    expect(keywords[1].dataset['description']).toBe(
      'Bleed(20) deals 20% of your Power every 2 seconds for 8 seconds.',
    );
  });

  it('recognizes adjective forms of keywords', () => {
    const root = render(
      formatter.format('Deal more damage to Slowed enemies.', [], () => 0),
    );
    const keyword = root.querySelector<HTMLElement>('.keyword');

    expect(keyword?.textContent).toBe('Slowed');
    expect(keyword?.dataset['title']).toBe('Slow');
  });

  it('keeps legacy placeholders working without adding a roll to modifiers', () => {
    const root = render(
      formatter.format(
        'Gain {Modify}.',
        [effect('ModifyAttribute', 'Power', 0.2, 5)],
        (attribute) => (attribute === 'Power' ? 100 : 0),
      ),
    );
    const modifier = root.querySelector<HTMLElement>('.mod');

    expect(modifier?.textContent).toBe('25');
    expect(modifier?.dataset['range']).toBe('false');
    expect(modifier?.dataset['unit']).toBe('value');
  });

  it('escapes authored markup before adding safe tooltip spans', () => {
    const root = render(
      formatter.format('<img src=x onerror=alert(1)> Slow', [], () => 0),
    );

    expect(root.querySelector('img')).toBeNull();
    expect(root.textContent).toContain('<img src=x onerror=alert(1)>');
    expect(root.querySelector('.keyword')?.textContent).toBe('Slow');
  });

  function effect(
    type: string,
    attribute: string,
    coefficient: number,
    baseValue = 0,
  ): EssenceEffectDto {
    return {
      id: `effect.${type.toLowerCase()}`,
      type,
      target: 'CurrentTarget',
      baseValue,
      currentValue: baseValue,
      scaling: [{ attribute, coefficient }],
      nestedEffects: [],
    };
  }

  function render(html: string): HTMLDivElement {
    const root = document.createElement('div');
    root.innerHTML = html;
    return root;
  }
});
