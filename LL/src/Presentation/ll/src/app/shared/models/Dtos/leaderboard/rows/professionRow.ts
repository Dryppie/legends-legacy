import { LeaderboardEntry, LeaderboardColumn } from '../leaderboardEntry';

export interface ProfessionRow extends LeaderboardEntry {
  characterName: string;
  level: number;
  experience: number;
}
export const PROFESSION_COLUMNS: readonly LeaderboardColumn<ProfessionRow>[] = [
  { header: 'Character', value: (r) => r.characterName },
  { header: 'Level', value: (r) => r.level, alignRight: true },
  { header: 'EXP', value: (r) => r.experience, alignRight: true },
];
