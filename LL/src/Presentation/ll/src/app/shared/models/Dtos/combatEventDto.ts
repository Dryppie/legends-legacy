import { AttributeType } from '../enums/attributeType';
import { SimpleCombatEntityDto } from './combatResultDto';

export enum EventType {
  AbilityUse = 'AbilityUse', // Dryp has cast {Ability}
  Damage = 'Damage', // Dryp took {Amount} damage
  DamageOverTime = 'DamageOverTime', // Dryp took {Amount} damage // Not sure this should be logged in chat
  DamageCrit = 'DamageCrit', // Dryp took {Amount} damage
  Miss = 'Miss', // Dryp missed
  Parry = 'Parry', // Dryp parried the attack
  Block = 'Block', // Dryp blocked the attack, and only took {Amount} damage
  Heal = 'Heal', // Dryp was healed for {Amount}
  HealOverTime = 'HealOverTime', // Dryp was healed for {Amount} // Not sure this should be logged in chat
  HealCrit = 'HealCrit', // Dryp was healed for {Amount}
  RestoreBarrier = 'RestoreBarrier', // Dryp received {Amount} barrier
  Lifesteal = 'Lifesteal', // Dryp gained {Amount} health through lifesteal
  Summon = 'Summon', // Imp has been summoned
  SummonExpired = 'SummonExpired', // Imp vanished. Summon effect expired.
  Buff = 'Buff', // Dryp's strength increased by {Amount}
  BuffExpired = 'BuffExpired',
  Debuff = 'Debuff', // Dryp's strength decreased by {Amount}
  DebuffExpired = 'DebuffExpired',
  StatusEffect = 'StatusEffect', // Dryp is stunned
  StatusEffectExpired = 'StatusEffectExpired', // Dryp is no longer stunned // Is not logged in chat. Maybe something visual instead?
  HealthRegeneration = 'HealthRegeneration',
  Death = 'Death',
}

export interface CombatEvent {
  eventType: EventType;
  attribute: AttributeType;
  magnitude: number;
  actorId: string;
  targetId: string;
  timestamp: number;
  details: string;
  combatEntity: SimpleCombatEntityDto;
}
