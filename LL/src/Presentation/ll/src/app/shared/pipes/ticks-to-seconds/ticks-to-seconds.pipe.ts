import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'ticksToSeconds',
  standalone: true,
})
export class TicksToSecondsPipe implements PipeTransform {
  transform(value: number): unknown {
    return value / 10;
  }
}
