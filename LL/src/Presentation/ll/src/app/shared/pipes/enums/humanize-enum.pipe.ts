import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'humanize',
  standalone: true,
})
export class HumanizeEnumPipe implements PipeTransform {
  transform(value: string): string {
    if (!value) return '';

    // Add spaces before capital letters (PascalCase or camelCase)
    return value
      .replace(/([a-z])([A-Z])/g, '$1 $2') // e.g. "SoulDust" → "Soul Dust"
      .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2') // e.g. "XMLHttp" → "XML Http"
      .trim();
  }
}
