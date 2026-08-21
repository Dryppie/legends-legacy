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

  it('uses combat-summary colors for damage types and damaging conditions', () => {
    const html = formatter.format(
      'Deal 90% Physical Damage and apply Burn(12) and Bleed(12).',
      [
        {
          id: 'effect.burning-mandibles.damage',
          type: 'Damage',
          target: 'CurrentTarget',
          baseValue: 0,
          currentValue: 0,
          attribute: null,
          status: null,
          durationSeconds: null,
          scaling: [{ attribute: 'Power', coefficient: 0.9 }],
          nestedEffects: [],
        },
      ],
      () => 100,
      'Burning Mandibles',
    );

    expect(html).toContain('class="dmg damage-type-physical"');
    expect(html).toContain('class="keyword damage-type-burn"');
    expect(html).toContain('class="keyword damage-type-bleed"');
  });

  it('colors damage-type phrases even when they have no authored magnitude', () => {
    const html = formatter.format(
      'Combusts for Magical Damage equal to Max Health.',
      [],
      () => 0,
    );

    expect(html).toContain('class="damage-type damage-type-magical"');
    expect(html).toContain('>Magical Damage</span>');
  });
});

describe('EssenceDescriptionFormatter ascension-scaled placeholders', () => {
  const formatter = new EssenceDescriptionFormatter();

  it('renders event, condition, and status coefficients from effect data', () => {
    const html = formatter.format(
      'Heal for {eventScaling}; add {conditionScaling}; then add {statusScaling}.',
      [
        {
          id: 'effect.event',
          type: 'Heal',
          target: 'Self',
          currentValue: 0,
          eventMagnitudeCoefficient: 0.055,
        },
        {
          id: 'effect.condition',
          type: 'Damage',
          target: 'CurrentTarget',
          currentValue: 0,
          conditionScalingCoefficient: 0.0224,
        },
        {
          id: 'effect.status',
          type: 'Damage',
          target: 'AllEnemies',
          currentValue: 0,
          statusScalingCoefficient: 0.224,
        },
      ],
      () => 0,
    );

    expect(html).toBe('Heal for 5.5%; add 2.24%; then add 22.4%.');
  });

  it('renders the selected scaled duration with its unit', () => {
    const html = formatter.format(
      'Gain Armor for {duration2}.',
      [
        {
          id: 'effect.first',
          type: 'ModifyAttribute',
          target: 'Self',
          currentValue: 0,
          durationSeconds: 3,
        },
        {
          id: 'effect.second',
          type: 'ModifyAttribute',
          target: 'Self',
          currentValue: 0,
          durationSeconds: 6.3,
        },
      ],
      () => 0,
    );

    expect(html).toBe('Gain Armor for 6.3 seconds.');
  });

  it('leaves an unresolved placeholder visible for diagnosis', () => {
    const html = formatter.format('Heal for {eventScaling}.', [], () => 0);

    expect(html).toBe('Heal for {eventScaling}.');
  });
});
