import { Pipe, PipeTransform } from '@angular/core';
import { getAttributeDefinition } from '../../../models/attribute-definition';

export function formatAttributeType(value: string): string {
  return (
    getAttributeDefinition(value)?.displayName ??
    value.replace(/([A-Z])/g, ' $1').trim()
  );
}

export function isPercentAttribute(value?: string | null): boolean {
  return getAttributeDefinition(value)?.unit === 'PercentagePoints';
}

export function formatAttributeTooltip(value: string): string {
  const definition = getAttributeDefinition(value);
  if (!definition) return formatAttributeType(value);

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
  transform(value: string): string {
    return formatAttributeType(value);
  }
}

@Pipe({
  name: 'attributeTooltip',
  standalone: true,
})
export class AttributeTooltipPipe implements PipeTransform {
  transform(value: string): string {
    return formatAttributeTooltip(value);
  }
}
