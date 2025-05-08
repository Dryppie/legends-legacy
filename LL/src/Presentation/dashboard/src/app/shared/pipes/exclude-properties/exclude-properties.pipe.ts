import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'excludeProperties',
  standalone: true,
})
export class ExcludePropertiesPipe implements PipeTransform {
  transform(
    obj: Record<string, any>,
    ...propertiesToExclude: string[]
  ): Array<{ key: string; value: any }> {
    return Object.keys(obj)
      .filter((key) => !propertiesToExclude.includes(key))
      .map((key) => ({ key, value: obj[key] }));
  }
}
