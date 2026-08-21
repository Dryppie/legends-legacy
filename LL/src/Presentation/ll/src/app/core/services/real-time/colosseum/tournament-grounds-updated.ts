export interface TournamentGroundsUpdated {
  tournamentId: string;
  stateVersion: number;
  tournamentNumber: number;
  tournamentName: string;
  event: string;
  status: string;
  registeredParticipantCount: number;
  minParticipants: number;
  maxParticipants: number;
  hasBracket: boolean;
  currentRoundNumber?: number | null;
  nextActionAtUtc?: string | null;
  completedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  occurredAtUtc: string;
}
