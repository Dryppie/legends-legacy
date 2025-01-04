import { Pipe, PipeTransform } from '@angular/core';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeDto } from '../../../models/Dtos/attributesDto';

@Pipe({
  name: 'secondaryAttributes',
  standalone: true,
})
export class SecondaryAttributesPipe implements PipeTransform {
  private secondaryAttributes = [
    AttributeType.Health,
    AttributeType.HealthRegeneration,
    AttributeType.Mana,
    AttributeType.ManaRegeneration,
    AttributeType.BasicAttackSpeed,
    AttributeType.Power,
    AttributeType.PhysicalDefense,
    AttributeType.MagicalDefense,
    AttributeType.DamageReduction,
    AttributeType.CritChance,
    AttributeType.CritDamage,
    AttributeType.CritDamageReduction,
    AttributeType.Threat,
    AttributeType.CrowdControlResistance,
    AttributeType.Accuracy,
    AttributeType.Dodge,
    AttributeType.Block,
    AttributeType.Parry,
    AttributeType.Barrier,
    AttributeType.MultiStrike,
    AttributeType.MultiCast,
    AttributeType.CooldownReduction,
    AttributeType.ArmorPenetration,
    AttributeType.ManaPenetration,
    AttributeType.LifeSteal,
    AttributeType.FireResistance,
    AttributeType.WaterResistance,
    AttributeType.EarthResistance,
    AttributeType.AirResistance,
    AttributeType.PoisonResistance,
  ];

  transform(values: AttributeDto[], ...args: unknown[]): AttributeDto[] {
    return values.filter((value) =>
      this.secondaryAttributes.includes(value.attributeType),
    );
  }
}
