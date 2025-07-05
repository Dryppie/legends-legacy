import { LeaderboardEntry, LeaderboardColumn } from '../leaderboardEntry';

export interface WealthRow extends LeaderboardEntry {
  characterName: string;
  cinders: number;
}

export const WEALTH_COLUMNS: readonly LeaderboardColumn<WealthRow>[] = [
  { header: 'Character', value: (r) => r.characterName },
  { header: 'Cinders', value: (r) => r.level, alignRight: true },
];
