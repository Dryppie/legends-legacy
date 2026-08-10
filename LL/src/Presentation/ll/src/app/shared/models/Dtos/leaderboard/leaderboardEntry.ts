export interface LeaderboardEntry {
  characterId: string;
  characterName: string;
  level: number;
  experience: number;
  rank: number;
}

export interface LeaderboardColumn<T extends LeaderboardEntry> {
  header: string; // Column header text
  value: (row: T) => string | number; // What to render
  isCharacterName?: boolean;
  cellClass?: string; // Tailwind / CSS classes
  alignRight?: boolean; // Right-align numeric values
}
