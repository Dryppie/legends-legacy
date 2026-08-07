import { AbilityTooltipComponent } from './ability-tooltip.component';
import { EssenceAbilityData } from './essenceAbilityData';

describe('AbilityTooltipComponent', () => {
  it('omits zero base damage and exposes the final Power and roll values', () => {
    const component = createComponent({
      base: 0,
      bonus: 14.85,
      attr: 'Power',
      attrValue: 11,
      scaleDisplay: '135%',
      total: '11-18',
      unit: 'damage',
      hasRange: true,
    });

    expect(component.hasBaseValue).toBeFalse();
    expect(component.displayedTotal).toBe('11–18');
    expect(component.rollDisplay).toBe('±20%');
  });

  it('keeps a meaningful non-zero base value available to the layout', () => {
    const component = createComponent({
      base: 5,
      bonus: 14.85,
      attr: 'Power',
      attrValue: 11,
      scaleDisplay: '135%',
      total: '15-24',
      unit: 'damage',
      hasRange: true,
    });

    expect(component.hasBaseValue).toBeTrue();
    expect(component.displayedTotal).toBe('15–24');
  });

  function createComponent(
    overrides: Partial<EssenceAbilityData>,
  ): AbilityTooltipComponent {
    return new AbilityTooltipComponent({
      kind: 'magnitude',
      title: 'Estimated damage',
      ...overrides,
    });
  }
});
