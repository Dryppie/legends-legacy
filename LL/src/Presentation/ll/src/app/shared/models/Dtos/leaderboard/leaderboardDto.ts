import { LeaderboardEntryDto } from './leaderboardEntryDto';

export interface LeaderboardDto {
  combat: LeaderboardEntryDto[];
  professions: Record<string, LeaderboardEntryDto[]>;
}
