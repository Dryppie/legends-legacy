import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'attributeTypeFormat',
  standalone: true,
})
export class AttributeTypeFormatPipe implements PipeTransform {
  transform(value: string): string {
    return value.replace(/([A-Z])/g, ' $1').trim();
  }
}
