import {
  AttributeModifier,
  ModifierType,
} from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { aggregateAttributes, sortAttributes } from './attribute-order.utils';

describe('sortAttributes', () => {
  it('uses the canonical AttributeType order', () => {
    const attributes = [
      modifier(AttributeType.Cooldown),
      modifier(AttributeType.Resistance),
      modifier(AttributeType.Power),
      modifier(AttributeType.MaxHealth),
      modifier(AttributeType.Spirit),
    ];

    expect(
      sortAttributes(attributes).map((attribute) => attribute.attributeType),
    ).toEqual([
      AttributeType.Power,
      AttributeType.Spirit,
      AttributeType.MaxHealth,
      AttributeType.Resistance,
      AttributeType.Cooldown,
    ]);
  });

  it('does not mutate the source array', () => {
    const attributes = [
      modifier(AttributeType.Resistance),
      modifier(AttributeType.Power),
    ];

    sortAttributes(attributes);

    expect(attributes.map((attribute) => attribute.attributeType)).toEqual([
      AttributeType.Resistance,
      AttributeType.Power,
    ]);
  });

  it('combines matching attributes into final values', () => {
    const attributes = aggregateAttributes([
      modifier(AttributeType.MaxHealth, 50),
      modifier(AttributeType.Spirit, 29),
      modifier(AttributeType.MaxHealth, 87),
    ]);

    expect(attributes).toEqual([
      modifier(AttributeType.Spirit, 29),
      modifier(AttributeType.MaxHealth, 137),
    ]);
  });

  it('keeps different stacking types as separate rows', () => {
    const attributes = aggregateAttributes([
      modifier(AttributeType.MaxHealth, 50),
      modifier(AttributeType.MaxHealth, 10, ModifierType.Additive),
    ]);

    expect(attributes.map((attribute) => attribute.modifierType)).toEqual([
      ModifierType.Flat,
      ModifierType.Additive,
    ]);
  });
});

function modifier(
  attributeType: AttributeType,
  amount = 1,
  modifierType = ModifierType.Flat,
): AttributeModifier {
  return {
    attributeType,
    amount,
    modifierType,
  };
}
