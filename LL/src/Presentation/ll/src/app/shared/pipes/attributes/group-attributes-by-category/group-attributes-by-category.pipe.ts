import { Pipe, PipeTransform } from '@angular/core';
import { AttributeType } from '../../../models/enums/attributeType';

export enum AttributeCategory {
  Offense = 'Offense',
  Defense = 'Defense',
  Recovery = 'Recovery',
  Utility = 'Utility',
}

interface AttributeWithType {
  attributeType: AttributeType;
}

const ATTRIBUTE_CATEGORIES: Record<AttributeType, AttributeCategory> = {
  [AttributeType.Power]: AttributeCategory.Offense,
  [AttributeType.CritChance]: AttributeCategory.Offense,
  [AttributeType.CritDamage]: AttributeCategory.Offense,
  [AttributeType.ArmorPenetration]: AttributeCategory.Offense,
  [AttributeType.MagicPenetration]: AttributeCategory.Offense,
  [AttributeType.AttackSpeed]: AttributeCategory.Offense,

  [AttributeType.MaxHealth]: AttributeCategory.Defense,
  [AttributeType.Armor]: AttributeCategory.Defense,
  [AttributeType.Resistance]: AttributeCategory.Defense,
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
};

export type AttributesByCategory<T extends AttributeWithType> = Record<
  AttributeCategory,
  T[]
>;

@Pipe({
  name: 'groupAttributesByCategory',
  standalone: true,
})
export class GroupAttributesByCategoryPipe implements PipeTransform {
  transform<T extends AttributeWithType>(
    attributes: readonly T[] | null | undefined,
  ): AttributesByCategory<T> {
    const grouped = Object.values(AttributeCategory).reduce(
      (categories, category) => {
        categories[category] = [];
        return categories;
      },
      {} as AttributesByCategory<T>,
    );

    for (const attribute of attributes ?? []) {
      grouped[ATTRIBUTE_CATEGORIES[attribute.attributeType]].push(attribute);
    }

    return grouped;
  }
}
