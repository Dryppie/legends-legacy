import { LeaderboardColumn, LeaderboardEntry } from '../leaderboardEntry';

export interface ArenaRow extends LeaderboardEntry {
  name: string;
  rating: number;
}

export const ARENA_COLUMNS: readonly LeaderboardColumn<ArenaRow>[] = [
  {
    header: 'Name',
    value: (r) => r.characterName,
    isCharacterName: true,
  },
  { header: 'Rating', value: (r) => r.level, alignRight: true },
];
