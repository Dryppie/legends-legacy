import { Pipe, PipeTransform } from '@angular/core';

export function formatAttributeType(value: string): string {
  const labels: Record<string, string> = {
    Power: 'Power',
    Fortitude: 'Fortitude',
    Precision: 'Precision',
    Spirit: 'Spirit',

    MaxHealth: 'Max Health',
    WeaponDamage: 'Weapon Damage',
    Armor: 'Armor',
    Resistance: 'Resistance',
    CritChance: 'Crit Chance',
    CritDamage: 'Crit Damage',
    ArmorPenetration: 'Armor Penetration',
    MagicPenetration: 'Magic Penetration',

    DodgeChance: 'Dodge',
    BlockChance: 'Block',
    DamageReduction: 'Damage Reduction',

    HealingPowerPercent: 'Healing Power',
    HealthRegeneration: 'Health Regen',
    LifeSteal: 'Life Steal',

    Cooldown: 'Cooldown Reduction',
    StatusResistance: 'Status Resistance',
    CrowdControlResistance: 'Crowd Control Resistance',

    SummonPower: 'Summon Power',
    SummonHealth: 'Summon Health',
  };

  return labels[value] ?? value.replace(/([A-Z])/g, ' $1').trim();
}

export function isPercentAttribute(value?: string | null): boolean {
  if (!value) return false;

  return [
    'CritChance',
    'CritDamage',
    'DodgeChance',
    'BlockChance',
    'DamageReduction',
    'HealingPowerPercent',
    'LifeSteal',
    'Cooldown',
    'StatusResistance',
    'CrowdControlResistance',
  ].includes(value);
}

@Pipe({
  name: 'attributeTypeFormat',
  standalone: true,
})
export class AttributeTypeFormatPipe implements PipeTransform {
  transform(value: string): string {
    return formatAttributeType(value);
  }
}
