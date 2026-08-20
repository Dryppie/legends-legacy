import { EssenceDescriptionFormatter } from './essence-description-formatter';

describe('EssenceDescriptionFormatter target explanations', () => {
  const formatter = new EssenceDescriptionFormatter();

  it('decorates all-allies text with its exact team inclusion rules', () => {
    const html = formatter.format('Grant all allies Barrier.', [], () => 0);

    expect(html).toContain('data-title="All allies"');
    expect(html).toContain('including the user and allied summons');
    expect(html).toContain('>all allies</span>');
  });

  it('decorates current-target text with its threat-selection rules', () => {
    const html = formatter.format('Damage the current target.', [], () => 0);

    expect(html).toContain('data-title="Current target"');
    expect(html).toContain('selected using threat');
    expect(html).toContain('>current target</span>');
  });

  it('prefers a full target phrase over its shorter alias', () => {
    const html = formatter.format('Damage up to three enemies.', [], () => 0);

    expect(html.match(/data-title=/g)?.length).toBe(1);
    expect(html).toContain('data-title="Three enemies"');
    expect(html).toContain('>up to three enemies</span>');
  });
});

describe('EssenceDescriptionFormatter magnitude coefficients', () => {
  const formatter = new EssenceDescriptionFormatter();

  it('keeps a fixed ascension-scaled coefficient as a single percentage', () => {
    const html = formatter.format(
      'Deal 120% Magical Damage and apply Burn(20).',
      [
        {
          id: 'effect.firebomb.damage',
          type: 'Damage',
          target: 'CurrentTarget',
          baseValue: 0,
          currentValue: 0,
          attribute: null,
          status: null,
          durationSeconds: null,
          scaling: [
            {
              attribute: 'Power',
              coefficient: 1.3440001010894775,
            },
          ],
          nestedEffects: [],
        },
      ],
      () => 33,
      'Firebomb Toss',
    );

    expect(html).toContain('>134.4% Magical Damage</span>');
    expect(html).not.toContain('134.4%-134.4%');
  });
});
