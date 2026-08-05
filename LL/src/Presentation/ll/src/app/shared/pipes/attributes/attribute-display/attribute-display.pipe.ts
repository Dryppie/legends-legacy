import { Pipe, PipeTransform } from '@angular/core';
import { AttributeModifier } from '../../../models/Dtos/attributesDto';
import { aggregateAttributes } from '../../../utils/attributes/attribute-order.utils';

@Pipe({
  name: 'attributeDisplay',
  standalone: true,
})
export class AttributeDisplayPipe implements PipeTransform {
  transform(
    baseModifiers: readonly AttributeModifier[] | null | undefined,
    instanceModifiers?: readonly AttributeModifier[] | null,
  ): AttributeModifier[] {
    return aggregateAttributes([
      ...(baseModifiers ?? []),
      ...(instanceModifiers ?? []),
    ]);
  }
}
