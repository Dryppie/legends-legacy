import { AttributeType } from '../../../models/enums/attributeType';
import {
  resolveEffectiveAttributeValue,
  resolveEffectiveThreatValue,
} from './essence-description.component';

describe('resolveEffectiveAttributeValue', () => {
  it('prefers the final equipped combat attribute over the raw base attribute', () => {
    expect(
      resolveEffectiveAttributeValue(
        'Power',
        [{ attributeType: AttributeType.Power, value: 11 }],
        [{ attributeType: AttributeType.Power, value: 10 }],
      ),
    ).toBe(11);
  });

  it('falls back to the base attribute before the combat overview is available', () => {
    expect(
      resolveEffectiveAttributeValue(
        'Power',
        [],
        [{ attributeType: AttributeType.Power, value: 10 }],
      ),
    ).toBe(10);
  });
});

describe('resolveEffectiveThreatValue', () => {
  it('applies the same ability multiplier used by combat', () => {
    expect(resolveEffectiveThreatValue(256, 1.5)).toBe(384);
  });

  it('rounds negative threat away from zero and clamps invalid multipliers', () => {
    expect(resolveEffectiveThreatValue(-5, 0.5)).toBe(-3);
    expect(resolveEffectiveThreatValue(100, -1)).toBe(0);
  });
});
