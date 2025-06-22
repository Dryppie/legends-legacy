import { LeaderboardEntryDto } from './leaderboardEntryDto';

export interface LeaderboardDto {
  totalLevel: LeaderboardEntryDto[];
  combat: LeaderboardEntryDto[];
  wealth: LeaderboardEntryDto[];
  professions: Record<string, LeaderboardEntryDto[]>;
}
