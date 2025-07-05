import { LeaderboardEntry } from './leaderboardEntry';

export interface Leaderboard {
  totalLevel: LeaderboardEntry[];
  combat: LeaderboardEntry[];
  wealth: LeaderboardEntry[];
  professions: Record<string, LeaderboardEntry[]>;
}
