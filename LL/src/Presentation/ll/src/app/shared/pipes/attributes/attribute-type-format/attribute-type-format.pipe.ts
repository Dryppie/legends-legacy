import { Pipe, PipeTransform } from '@angular/core';
import { getAttributeDefinition } from '../../../models/attribute-definition';

export function formatAttributeType(
  value: string,
  equipmentRating = false,
): string {
  const definition = getAttributeDefinition(value);
  return (
    (equipmentRating
      ? definition?.equipmentDisplayName
      : definition?.displayName) ??
    value.replace(/([A-Z])/g, ' $1').trim()
  );
}

export function isPercentAttribute(value?: string | null): boolean {
  return getAttributeDefinition(value)?.unit === 'PercentagePoints';
}

export function isEquipmentRatingAttribute(value?: string | null): boolean {
  return getAttributeDefinition(value)?.equipmentUnit === 'Rating';
}

export function formatAttributeTooltip(
  value: string,
  equipmentRating = false,
): string {
  const definition = getAttributeDefinition(value);
  if (!definition) return formatAttributeType(value, equipmentRating);

  if (equipmentRating)
    return definition.equipmentDescription ?? definition.description;

  const cap =
    definition.capKind === 'Fixed' && definition.maximumValue != null
      ? ` Cap: ${definition.maximumValue}${definition.displaySuffix}.`
      : definition.capKind === 'ContextDependent'
        ? ' Cap depends on combat context.'
        : '';

  return `${definition.description}${cap}`;
}

@Pipe({
  name: 'attributeTypeFormat',
  standalone: true,
})
export class AttributeTypeFormatPipe implements PipeTransform {
  transform(value: string, equipmentRating = false): string {
    return formatAttributeType(value, equipmentRating);
  }
}

@Pipe({
  name: 'attributeTooltip',
  standalone: true,
})
export class AttributeTooltipPipe implements PipeTransform {
  transform(value: string, equipmentRating = false): string {
    return formatAttributeTooltip(value, equipmentRating);
  }
}
