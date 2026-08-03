import { Pipe, PipeTransform } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { AttributeType } from '../../../models/enums/attributeType';

export enum AttributeCategory {
  Offensive = 'Offensive',
  Defensive = 'Defensive',
  Vitality = 'Vitality',
  Recovery = 'Recovery',
  Utility = 'Utility',
  Summons = 'Summons',
}

@Pipe({
  name: 'groupAttributesByCategory',
  standalone: true,
  pure: true,
})
export class GroupAttributesByCategoryPipe implements PipeTransform {
  ATTRIBUTE_CATEGORY_MAP: Record<AttributeType, AttributeCategory> = {
    [AttributeType.Power]: AttributeCategory.Offensive,
    [AttributeType.Fortitude]: AttributeCategory.Vitality,
    [AttributeType.Precision]: AttributeCategory.Offensive,
    [AttributeType.Spirit]: AttributeCategory.Vitality,

    [AttributeType.MaxHealth]: AttributeCategory.Vitality,
    [AttributeType.Armor]: AttributeCategory.Defensive,
    [AttributeType.Resistance]: AttributeCategory.Defensive,
    [AttributeType.CritChance]: AttributeCategory.Offensive,
    [AttributeType.CritDamage]: AttributeCategory.Offensive,
    [AttributeType.ArmorPenetration]: AttributeCategory.Offensive,
    [AttributeType.MagicPenetration]: AttributeCategory.Offensive,

    [AttributeType.DodgeChance]: AttributeCategory.Defensive,
    [AttributeType.BlockChance]: AttributeCategory.Defensive,
    [AttributeType.DamageReduction]: AttributeCategory.Defensive,

    [AttributeType.HealingPowerPercent]: AttributeCategory.Recovery,
    [AttributeType.HealthRegeneration]: AttributeCategory.Recovery,
    [AttributeType.LifeSteal]: AttributeCategory.Recovery,

    [AttributeType.Cooldown]: AttributeCategory.Utility,
    [AttributeType.StatusResistance]: AttributeCategory.Utility,
    [AttributeType.CrowdControlResistance]: AttributeCategory.Utility,

    [AttributeType.SummonPower]: AttributeCategory.Summons,
    [AttributeType.SummonHealth]: AttributeCategory.Summons,
    [AttributeType.AttackSpeed]: AttributeCategory.Offensive,
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
