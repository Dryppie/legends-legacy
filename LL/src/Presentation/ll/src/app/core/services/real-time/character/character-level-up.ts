export interface CharacterLevelUpMsg {
  characterId: string;
  level: number;
  experience: number;
  experienceUntilNextLevel: number;
}
