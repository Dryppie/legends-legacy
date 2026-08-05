export interface CharacterProfession {
  professionType: ProfessionType;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
}

export enum ProfessionType {
  // Crafting
  Crafting = 'Crafting',

  // Gathering
  Mining = 'Mining',
  Woodcutting = 'Woodcutting',
  Skinning = 'Skinning',
}
