import { Pipe, PipeTransform } from '@angular/core';

const equipmentNames: Readonly<Record<string, string>> = {
  MaxHealth: 'Max Health',
  Armor: 'Armor',
  Resistance: 'Resistance',
  CritChance: 'Critical Chance',
  CritDamage: 'Critical Damage',
  ArmorPenetration: 'Armor Penetration',
  MagicPenetration: 'Magic Penetration',
  DodgeChance: 'Dodge Chance',
  BlockChance: 'Block Chance',
  DamageReduction: 'Damage Reduction',
  HealingPowerPercent: 'Healing Power',
  HealthRegeneration: 'Health Regen',
  LifeSteal: 'Life Steal',
  Cooldown: 'Cooldown Reduction',
  StatusResistance: 'Status Resistance',
  CrowdControlResistance: 'Crowd Control Resistance',
  AttackSpeed: 'Attack Speed',
};

const directPercentages = new Set([
  'CritChance',
  'CritDamage',
  'ArmorPenetration',
  'MagicPenetration',
  'DodgeChance',
  'BlockChance',
  'DamageReduction',
  'HealingPowerPercent',
  'LifeSteal',
  'Cooldown',
  'StatusResistance',
  'CrowdControlResistance',
  'AttackSpeed',
]);

@Pipe({
  name: 'attributeTypeFormat',
  standalone: true,
})
export class AttributeTypeFormatPipe implements PipeTransform {
  transform(value: string, equipment = false): string {
    if (equipment && equipmentNames[value]) return equipmentNames[value];
    return value.replace(/([A-Z])/g, ' $1').trim();
  }
}

@Pipe({
  name: 'attributeValueFormat',
  standalone: true,
})
export class AttributeValueFormatPipe implements PipeTransform {
  transform(value: number, attributeType: string, equipment = false): string {
    const suffix = equipment && directPercentages.has(attributeType) ? '%' : '';
    return `${value}${suffix}`;
  }
}
