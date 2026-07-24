import { Pipe, PipeTransform } from '@angular/core';
import { getAttributeDefinition } from '../../../models/attribute-definition';

export function formatAttributeValue(
  value: number | null | undefined,
  attribute?: string | null,
  signed = false,
): string {
  const amount = value ?? 0;
  const sign = signed && amount > 0 ? '+' : '';
  const definition = getAttributeDefinition(attribute);
  const suffix = definition?.displaySuffix ?? '';
  const precision = definition?.displayPrecision ?? 2;

  return `${sign}${formatNumber(amount, precision)}${suffix}`;
}

@Pipe({
  name: 'attributeValueFormat',
  standalone: true,
})
export class AttributeValueFormatPipe implements PipeTransform {
  transform(
    value: number | null | undefined,
    attribute?: string | null,
    signed = false,
  ): string {
    return formatAttributeValue(value, attribute, signed);
  }
}

function formatNumber(value: number, precision: number): string {
  return Number.isInteger(value)
    ? `${value}`
    : value.toFixed(precision).replace(/\.?0+$/, '');
}
