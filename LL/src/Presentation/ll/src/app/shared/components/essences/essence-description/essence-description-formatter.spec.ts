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
