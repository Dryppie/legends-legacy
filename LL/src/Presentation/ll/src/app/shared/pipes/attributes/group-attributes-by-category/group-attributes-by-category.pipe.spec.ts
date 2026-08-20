import { AttributeType } from '../../../models/enums/attributeType';
import {
  AttributeCategory,
  GroupAttributesByCategoryPipe,
} from './group-attributes-by-category.pipe';

describe('GroupAttributesByCategoryPipe', () => {
  const pipe = new GroupAttributesByCategoryPipe();

  it('groups the flat attribute model by combat purpose', () => {
    const grouped = pipe.transform(
      Object.values(AttributeType).map((attributeType) => ({
        attributeType,
        value: 1,
      })),
    );

    expect(types(grouped[AttributeCategory.Offense])).toContain(
      AttributeType.Power,
    );
    expect(types(grouped[AttributeCategory.Offense])).toContain(
      AttributeType.AttackSpeed,
    );
    expect(types(grouped[AttributeCategory.Defense])).toContain(
      AttributeType.MaxHealth,
    );
    expect(types(grouped[AttributeCategory.Recovery])).toContain(
      AttributeType.HealingPowerPercent,
    );
    expect(types(grouped[AttributeCategory.Utility])).toContain(
      AttributeType.Cooldown,
    );
    expect(types(grouped[AttributeCategory.Utility])).toContain(
      AttributeType.Threat,
    );

    expect(Object.values(grouped).flat()).toHaveSize(
      Object.values(AttributeType).length,
    );
  });
});

function types(
  attributes: { attributeType: AttributeType }[] | undefined,
): AttributeType[] {
  return (attributes ?? []).map((attribute) => attribute.attributeType);
}
