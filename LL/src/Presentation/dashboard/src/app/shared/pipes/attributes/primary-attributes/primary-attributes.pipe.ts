import { Pipe, PipeTransform } from '@angular/core';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeDto } from '../../../models/Dtos/attributesDto';

@Pipe({
  name: 'primaryAttributes',
  standalone: true,
})
export class PrimaryAttributesPipe implements PipeTransform {
  private primaryAttributes = [
    AttributeType.Constitution,
    AttributeType.Endurance,
    AttributeType.Willpower,
    AttributeType.Strength,
    AttributeType.FightingSpirit,
    AttributeType.Dexterity,
    AttributeType.Agility,
    AttributeType.Intelligence,
    AttributeType.Wisdom,
    AttributeType.Instinct,
    AttributeType.Perception,
    AttributeType.Luck,
  ];

  transform(values: AttributeDto[], ...args: unknown[]): AttributeDto[] {
    return values.filter((value) =>
      this.primaryAttributes.includes(value.attributeType),
    );
  }
}
