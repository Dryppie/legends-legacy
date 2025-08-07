import { Pipe, PipeTransform } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { AttributeType } from '../../../models/enums/attributeType';

export enum AttributeCategory {
  Offensive = 'Offensive',
  Defensive = 'Defensive',
  Vitality = 'Vitality',
  Resistance = 'Resistance',
  Utility = 'Utility',
  None = 'None',
}

@Pipe({
  name: 'groupAttributesByCategory',
  standalone: true,
  pure: true,
})
export class GroupAttributesByCategoryPipe implements PipeTransform {
  ATTRIBUTE_CATEGORY_MAP: Record<AttributeType, AttributeCategory> = {
    // Vitality
    [AttributeType.Health]: AttributeCategory.Vitality,
    [AttributeType.HealthRegeneration]: AttributeCategory.Vitality,
    [AttributeType.Mana]: AttributeCategory.Vitality,
    [AttributeType.ManaRegeneration]: AttributeCategory.Vitality,
    [AttributeType.Barrier]: AttributeCategory.Vitality,
    [AttributeType.MaxHealth]: AttributeCategory.None,
    [AttributeType.MaxMana]: AttributeCategory.None,
    [AttributeType.RecoveryRate]: AttributeCategory.Vitality,

    // Offensive
    [AttributeType.AttackPower]: AttributeCategory.Offensive,
    [AttributeType.SpellPower]: AttributeCategory.Offensive,
    [AttributeType.CritChance]: AttributeCategory.Offensive,
    [AttributeType.CritDamage]: AttributeCategory.Offensive,
    [AttributeType.MultiStrike]: AttributeCategory.Offensive,
    [AttributeType.MultiCast]: AttributeCategory.Offensive,
    [AttributeType.ArmorPenetration]: AttributeCategory.Offensive,
    [AttributeType.ManaPenetration]: AttributeCategory.Offensive,
    [AttributeType.Accuracy]: AttributeCategory.Offensive,
    [AttributeType.AttackSpeed]: AttributeCategory.Offensive,

    // Defensive
    [AttributeType.PhysicalDefense]: AttributeCategory.Defensive,
    [AttributeType.MagicalDefense]: AttributeCategory.Defensive,
    [AttributeType.DamageReduction]: AttributeCategory.Defensive,
    [AttributeType.CritDamageReduction]: AttributeCategory.Defensive,
    [AttributeType.Dodge]: AttributeCategory.Defensive,
    [AttributeType.Block]: AttributeCategory.Defensive,
    [AttributeType.Parry]: AttributeCategory.Defensive,
    [AttributeType.CrowdControlResistance]: AttributeCategory.Defensive,

    // Resistance
    [AttributeType.FireResistance]: AttributeCategory.Resistance,
    [AttributeType.WaterResistance]: AttributeCategory.Resistance,
    [AttributeType.EarthResistance]: AttributeCategory.Resistance,
    [AttributeType.AirResistance]: AttributeCategory.Resistance,

    // Utility
    [AttributeType.CooldownReduction]: AttributeCategory.Utility,
    [AttributeType.Threat]: AttributeCategory.Utility,
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
