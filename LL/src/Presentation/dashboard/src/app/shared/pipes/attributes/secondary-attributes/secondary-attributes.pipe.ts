import { Pipe, PipeTransform } from '@angular/core';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeDto } from '../../../models/Dtos/attributesDto';

@Pipe({
  name: 'secondaryAttributes',
  standalone: true,
})
export class SecondaryAttributesPipe implements PipeTransform {
  private secondaryAttributes = [
    AttributeType.MaxHealth,
    AttributeType.Armor,
    AttributeType.Resistance,
    AttributeType.CritChance,
    AttributeType.CritDamage,
    AttributeType.ArmorPenetration,
    AttributeType.MagicPenetration,
    AttributeType.DodgeChance,
    AttributeType.BlockChance,
    AttributeType.DamageReduction,
    AttributeType.HealingPowerPercent,
    AttributeType.HealthRegeneration,
    AttributeType.LifeSteal,
    AttributeType.Cooldown,
    AttributeType.StatusResistance,
    AttributeType.CrowdControlResistance,
    AttributeType.AttackSpeed,
  ];

  transform(values: AttributeDto[], ...args: unknown[]): AttributeDto[] {
    return values.filter((value) =>
      this.secondaryAttributes.includes(value.attributeType),
    );
  }
}
