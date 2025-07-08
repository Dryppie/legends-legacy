export enum AttributeType {
  /* ===== VITALITY ===== */
  MaxHealth = 'MaxHealth',
  Health = 'Health',
  HealthRegeneration = 'HealthRegeneration',
  MaxMana = 'MaxMana',
  Mana = 'Mana',
  ManaRegeneration = 'ManaRegeneration',
  RecoveryRate = 'RecoveryRate',
  Barrier = 'Barrier',

  /* ===== OFFENSE ===== */
  AttackPower = 'AttackPower',
  SpellPower = 'SpellPower',
  AttackSpeed = 'AttackSpeed',
  Accuracy = 'Accuracy',
  CritChance = 'CritChance',
  CritDamage = 'CritDamage',
  MultiStrike = 'MultiStrike',
  MultiCast = 'MultiCast',
  ArmorPenetration = 'ArmorPenetration',
  ManaPenetration = 'ManaPenetration',

  /* ===== DEFENSE ===== */
  PhysicalDefense = 'PhysicalDefense',
  MagicalDefense = 'MagicalDefense',
  DamageReduction = 'DamageReduction',
  CritDamageReduction = 'CritDamageReduction',
  CrowdControlResistance = 'CrowdControlResistance',
  Dodge = 'Dodge',
  Block = 'Block',
  Parry = 'Parry',

  /* ===== CONTROL & UTILITY ===== */
  Threat = 'Threat',
  CooldownReduction = 'CooldownReduction',

  /* ===== RESISTANCES ===== */
  FireResistance = 'FireResistance',
  WaterResistance = 'WaterResistance',
  EarthResistance = 'EarthResistance',
  AirResistance = 'AirResistance',
}
