import { Pipe, PipeTransform } from '@angular/core';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeDto } from '../../../models/Dtos/attributesDto';

@Pipe({
  name: 'primaryAttributes',
  standalone: true,
})
export class PrimaryAttributesPipe implements PipeTransform {
  private primaryAttributes = [AttributeType.Power];

  transform(values: AttributeDto[], ...args: unknown[]): AttributeDto[] {
    return values.filter((value) =>
      this.primaryAttributes.includes(value.attributeType),
    );
  }
}
