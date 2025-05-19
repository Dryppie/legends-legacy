export interface CharacterProfession {
  professionType: ProfessionType;
  level: number;
  experience: number;
}

export enum ProfessionType {
  // Crafting
  ArmorForging = 'ArmorForging',
  JewelryCrafting = 'JewelryCrafting',
  WeaponSmithing = 'WeaponSmithing',

  // Gathering
  Fishing = 'Fishing',
  Mining = 'Mining',
  Woodcutting = 'Woodcutting',
}
