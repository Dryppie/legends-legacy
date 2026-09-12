export enum Rarity {
  Common = 'Common',
  Uncommon = 'Uncommon',
  Rare = 'Rare',
  Epic = 'Epic',
  Unique = 'Unique',
  Legendary = 'Legendary',
  Legacy = 'Legacy',
}

export const RARITY_CODES: Record<Rarity, string> = {
  [Rarity.Common]: 'C',
  [Rarity.Uncommon]: 'U',
  [Rarity.Rare]: 'R',
  [Rarity.Epic]: 'E',
  [Rarity.Unique]: 'U',
  [Rarity.Legendary]: 'L',
  [Rarity.Legacy]: 'LG',
};
