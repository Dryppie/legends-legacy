import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'shortNumber',
  standalone: true, // if using Angular standalone APIs
})
export class ShortNumberPipe implements PipeTransform {
  transform(value: number): string {
    if (value === null || value === undefined) return '';

    const abs = Math.abs(value);
    if (abs < 1_000) return value.toString();
    if (abs < 1_000_000)
      return (value / 1_000).toFixed(1).replace(/\.0$/, '') + 'K';
    if (abs < 1_000_000_000)
      return (value / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
    if (abs < 1_000_000_000_000)
      return (value / 1_000_000_000).toFixed(1).replace(/\.0$/, '') + 'B';
    return (value / 1_000_000_000_000).toFixed(1).replace(/\.0$/, '') + 'T';
  }
}
