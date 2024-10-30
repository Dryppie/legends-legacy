export enum EventType {
  AbilityUse, // Dryp has cast {Ability}
  Damage, // Dryp took {Amount} damage
  DamageOverTime, // Dryp took {Amount} damage // Not sure this should be logged in chat
  DamageCrit, // Dryp took {Amount} damage
  Miss, // Dryp missed
  Parry, // Dryp parried the attack
  Block, // Dryp blocked the attack, and only took {Amount} damage
  Heal, // Dryp was healed for {Amount}
  HealOverTime, // Dryp was healed for {Amount} // Not sure this should be logged in chat
  HealCrit, // Dryp was healed for {Amount}
  Summon, // Imp has been summoned
  SummonExpired, // Imp vanished. Summon effect expired.
  Buff, // Dryp's strength increased by {Amount}
  BuffExpired,
  Debuff, // Dryp's strength decreased by {Amount}
  DebuffExpired,
  StatusEffect, // Dryp is stunned
  StatusEffectExpired, // Dryp is no longer stunned // Is not logged in chat. Maybe something visual instead?
  Regeneration, // Natural regeneration every x seconds // Is not logged in chat
  // Add other event types like Lifesteal if needed
}

export enum ResourceType {
  Mana,
  Health,
}

export interface CombatEvent {
  eventType: EventType;
  resourceType: ResourceType;
  magnitude: number;
  actorId: string;
  targetId: string;
  timestamp: number;
  details: string;
}
