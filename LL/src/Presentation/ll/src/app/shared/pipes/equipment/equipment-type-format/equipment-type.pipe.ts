import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'equipmentType',
  standalone: true,
})
export class EquipmentTypePipe implements PipeTransform {
  transform(value: string): string {
    return value.replace(/([A-Z])/g, ' $1').trim();
  }
}
