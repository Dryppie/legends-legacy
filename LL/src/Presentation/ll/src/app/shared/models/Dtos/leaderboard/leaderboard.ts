export interface LeaderboardBoardEntry {
  participantId: string;
  participantName: string;
  rank: number;
  primaryValue: number;
  secondaryValue: number | null;
}

export interface LeaderboardBoard {
  key: string;
  category: string;
  title: string;
  description: string;
  participantLabel: string;
  metricLabel: string;
  secondaryMetricLabel: string | null;
  periodLabel: string;
  updatedAt: string;
  totalParticipants: number;
  pageStartRank: number;
  pageEndRank: number;
  previousCursor: string | null;
  nextCursor: string | null;
  searchQuery: string | null;
  searchMatch: LeaderboardBoardEntry | null;
  isViewerRanked: boolean;
  viewerUnrankedReason: string | null;
  entries: LeaderboardBoardEntry[];
  viewerEntry: LeaderboardBoardEntry | null;
}
