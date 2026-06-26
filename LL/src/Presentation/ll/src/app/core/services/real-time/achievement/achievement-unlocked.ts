export interface AchievementUnlockedMsg {
  characterId?: string | null;
  achievementKey: string;
  achievementName: string;
  points: number;
  titleKey?: string | null;
  titleName?: string | null;
  message: string;
  isGlobal: boolean;
}
