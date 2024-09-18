import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../../environments/environment';

@Pipe({
  name: 'attributeInfo',
  standalone: true,
})
export class AttributeInfoPipe implements PipeTransform {
  transform(attributeName: string): string {
    switch (attributeName.toLowerCase()) {
      case 'constitution':
        return 'Tooltip text for constitution';
      case 'endurance':
        return 'Tooltip text for endurance';
      case 'willpower':
        return 'Tooltip text for willpower';
      case 'strength':
        return 'Tooltip text for strength';
      case 'fighting spirit':
        return 'Tooltip text for fighting spirit';
      case 'dexterity':
        return 'Tooltip text for dexterity';
      case 'agility':
        return 'Tooltip text for agility';
      case 'intelligence':
        return 'Tooltip text for intelligence';
      case 'wisdom':
        return 'Tooltip text for wisdom';
      case 'perception':
        return 'Tooltip text for perception';
      case 'luck':
        return 'Tooltip text for luck';
      default:
        return environment.errorMessage;
    }
  }
}
