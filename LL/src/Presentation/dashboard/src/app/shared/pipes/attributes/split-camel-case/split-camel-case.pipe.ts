import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'splitCamelCase',
  standalone: true
})
export class SplitCamelCasePipe implements PipeTransform {

  transform(value: string): string {
    // Split the string at every capital letter and join with spaces
    return value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

}
