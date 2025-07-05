import { LeaderboardEntry, LeaderboardColumn } from '../leaderboardEntry';

export interface TotalLevelRow extends LeaderboardEntry {
  characterName: string;
  totalLevel: number;
}

export const TOTAL_LEVEL_COLUMNS: readonly LeaderboardColumn<TotalLevelRow>[] =
  [
    { header: 'Character', value: (r) => r.characterName },
    { header: 'Level', value: (r) => r.level, alignRight: true },
  ];
