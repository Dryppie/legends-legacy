import { Pipe, PipeTransform } from '@angular/core';
import { isPercentAttribute } from '../attribute-type-format/attribute-type-format.pipe';

export function formatAttributeValue(
  value: number | null | undefined,
  attribute?: string | null,
  signed = false,
): string {
  const amount = value ?? 0;
  const sign = signed && amount > 0 ? '+' : '';
  const suffix = isPercentAttribute(attribute) ? '%' : '';

  return `${sign}${formatNumber(amount)}${suffix}`;
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

function formatNumber(value: number): string {
  return Number.isInteger(value)
    ? `${value}`
    : value.toFixed(2).replace(/\.?0+$/, '');
}
