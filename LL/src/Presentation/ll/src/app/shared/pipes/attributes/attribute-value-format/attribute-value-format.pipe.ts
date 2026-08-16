import { Pipe, PipeTransform } from '@angular/core';
import { getAttributeDefinition } from '../../../models/attribute-definition';

export function formatAttributeValue(
  value: number | null | undefined,
  attribute?: string | null,
  signed = false,
  equipmentRating = false,
): string {
  const amount = value ?? 0;
  const sign = signed && amount > 0 ? '+' : '';
  const definition = getAttributeDefinition(attribute);
  const suffix = equipmentRating
    ? (definition?.equipmentDisplaySuffix ?? '')
    : (definition?.displaySuffix ?? '');
  const precision = equipmentRating
    ? (definition?.equipmentDisplayPrecision ?? 2)
    : (definition?.displayPrecision ?? 2);

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
    equipmentRating = false,
  ): string {
    return formatAttributeValue(value, attribute, signed, equipmentRating);
  }
}

function formatNumber(value: number, precision: number): string {
  if (Number.isInteger(value)) return `${value}`;

  const formatted = value.toFixed(precision);
  return precision === 0 ? formatted : formatted.replace(/\.?0+$/, '');
}
