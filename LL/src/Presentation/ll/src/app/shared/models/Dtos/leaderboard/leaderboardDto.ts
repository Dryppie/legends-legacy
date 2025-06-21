import { LeaderboardEntryDto } from './leaderboardEntryDto';

export interface LeaderboardDto {
  combat: LeaderboardEntryDto[];
  wealth: LeaderboardEntryDto[];
  professions: Record<string, LeaderboardEntryDto[]>;
}
