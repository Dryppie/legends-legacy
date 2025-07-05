import { LeaderboardEntry, LeaderboardColumn } from '../leaderboardEntry';

export interface CombatRow extends LeaderboardEntry {
  characterName: string;
  combatLevel: number;
  experience: number;
}
export const COMBAT_COLUMNS: readonly LeaderboardColumn<CombatRow>[] = [
  { header: 'Character', value: (r) => r.characterName },
  { header: 'Combat Lv', value: (r) => r.level, alignRight: true },
  { header: 'EXP', value: (r) => r.experience, alignRight: true },
];
