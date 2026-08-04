import { AttributeModifier } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';

interface AttributeWithType {
  attributeType: AttributeType;
}

const ATTRIBUTE_ORDER = Object.values(AttributeType);

export function aggregateAttributes(
  modifiers: readonly AttributeModifier[],
): AttributeModifier[] {
  const attributes = new Map<string, AttributeModifier>();

  for (const modifier of modifiers) {
    const key = `${modifier.attributeType}:${modifier.modifierType}`;
    const attribute = attributes.get(key);

    if (attribute) {
      attribute.amount += modifier.amount;
    } else {
      attributes.set(key, { ...modifier });
    }
  }

  return sortAttributes([...attributes.values()]);
}

export function sortAttributes<T extends AttributeWithType>(
  attributes: readonly T[],
): T[] {
  return [...attributes].sort((a, b) => {
    const orderDelta =
      getAttributeOrder(a.attributeType) - getAttributeOrder(b.attributeType);

    return orderDelta || a.attributeType.localeCompare(b.attributeType);
  });
}

function getAttributeOrder(attribute: AttributeType): number {
  const index = ATTRIBUTE_ORDER.indexOf(attribute);
  return index === -1 ? Number.MAX_SAFE_INTEGER : index;
}
