import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'numberFormat',
  standalone: true,
})
export class NumberFormatPipe implements PipeTransform {
  transform(value: number | undefined): string {
    if (!value && value !== 0) {
      return '';
    }

    // Use the browser's current locale to format the number
    return value.toLocaleString();
  }
}
