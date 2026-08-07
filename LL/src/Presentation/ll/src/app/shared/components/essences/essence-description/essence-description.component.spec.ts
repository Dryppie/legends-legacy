import { AttributeType } from '../../../models/enums/attributeType';
import { resolveEffectiveAttributeValue } from './essence-description.component';

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
