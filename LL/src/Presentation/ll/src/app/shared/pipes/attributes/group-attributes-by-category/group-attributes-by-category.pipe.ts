import { Pipe, PipeTransform } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { AttributeType } from '../../../models/enums/attributeType';

export enum AttributeCategory {
  Offense = 'Offense',
  Defense = 'Defense',
  Recovery = 'Recovery',
  Utility = 'Utility',
}

@Pipe({
  name: 'groupAttributesByCategory',
  standalone: true,
  pure: true,
})
export class GroupAttributesByCategoryPipe implements PipeTransform {
  ATTRIBUTE_CATEGORY_MAP: Record<AttributeType, AttributeCategory> = {
    [AttributeType.Power]: AttributeCategory.Offense,

    [AttributeType.MaxHealth]: AttributeCategory.Defense,
    [AttributeType.Armor]: AttributeCategory.Defense,
    [AttributeType.Resistance]: AttributeCategory.Defense,
    [AttributeType.CritChance]: AttributeCategory.Offense,
    [AttributeType.CritDamage]: AttributeCategory.Offense,
    [AttributeType.ArmorPenetration]: AttributeCategory.Offense,
    [AttributeType.MagicPenetration]: AttributeCategory.Offense,

    [AttributeType.DodgeChance]: AttributeCategory.Defense,
    [AttributeType.BlockChance]: AttributeCategory.Defense,
    [AttributeType.DamageReduction]: AttributeCategory.Defense,

    [AttributeType.HealingPowerPercent]: AttributeCategory.Recovery,
    [AttributeType.HealthRegeneration]: AttributeCategory.Recovery,
    [AttributeType.LifeSteal]: AttributeCategory.Recovery,

    [AttributeType.Cooldown]: AttributeCategory.Utility,
    [AttributeType.StatusResistance]: AttributeCategory.Utility,
    [AttributeType.CrowdControlResistance]: AttributeCategory.Utility,
    [AttributeType.Threat]: AttributeCategory.Utility,

    [AttributeType.AttackSpeed]: AttributeCategory.Offense,
  };

  transform(values: AttributeDto[]): Record<string, AttributeDto[]> {
    const grouped: Record<string, AttributeDto[]> = {};

    for (const attr of values) {
      const category = this.ATTRIBUTE_CATEGORY_MAP[attr.attributeType];
      if (!category) {
        console.warn('Uncategorized attribute type:', attr.attributeType);
        continue;
      }

      if (!grouped[category]) {
        grouped[category] = [];
      }

      grouped[category].push(attr);
    }

    return grouped;
  }
}
