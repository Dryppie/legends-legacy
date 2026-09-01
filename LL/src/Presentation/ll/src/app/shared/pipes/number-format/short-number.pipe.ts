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
    if (abs < 1_000_000) return this.abbreviate(value, 1_000, 'K');
    if (abs < 1_000_000_000) return this.abbreviate(value, 1_000_000, 'M');
    if (abs < 1_000_000_000_000)
      return this.abbreviate(value, 1_000_000_000, 'B');
    return this.abbreviate(value, 1_000_000_000_000, 'T');
  }

  private abbreviate(value: number, divisor: number, suffix: string): string {
    const sign = Math.sign(value);
    const truncated = (Math.floor((Math.abs(value) / divisor) * 10) / 10) * sign;
    return `${truncated.toFixed(1).replace(/\.0$/, '')}${suffix}`;
  }
}
