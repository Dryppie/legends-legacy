import {
  AbilityDamageTypeStats,
  BattleOutcome,
  EntityStats,
  SimpleCombatEntityDto,
} from '../combatResultDto';
import { InventoryItem } from '../../inventoryItem';

export interface TournamentGroundsStatus {
  nowUtc: string;
  currentTournament: TournamentSummary | null;
  upcomingTournaments: TournamentSummary[];
  recentTournaments: TournamentSummary[];
  developmentToolsEnabled: boolean;
}

export interface TournamentSummary {
  id: string;
  name: string;
  status: string;
  registrationStartsAtUtc: string;
  registrationEndsAtUtc: string;
  startsAtUtc: string;
  registeredParticipantCount: number;
  minParticipants: number;
  maxParticipants: number;
  isRegistered: boolean;
  canRegister: boolean;
  cannotRegisterReason?: string | null;
  playerParticipantId?: string | null;
  hasUnclaimedRewards: boolean;
  playerStatus?: string | null;
  playerSeed?: number | null;
  playerEntryArenaRating?: number | null;
  playerFinalPlacement?: number | null;
  completedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
}

export interface TournamentDetails {
  summary: TournamentSummary;
  participants: TournamentParticipant[];
  teams: TournamentTeam[];
  rewards: TournamentRewardGrant[];
}

export interface TournamentHistoryEntry {
  tournamentId: string;
  tournamentNumber: number;
  tournamentName: string;
  status: string;
  completedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
  participantId: string;
  seed?: number | null;
  entryArenaRating: number;
  entryRankTier: string;
  participantStatus: string;
  finalPlacement?: number | null;
  rewardStatus?: string | null;
  replayCount: number;
}

export interface TournamentHallOfFameEntry {
  tournamentId: string;
  tournamentNumber: number;
  tournamentName: string;
  completedAtUtc: string;
  participantCount: number;
  championParticipantId: string;
  championCharacterId: string;
  championName: string;
  championSeed?: number | null;
  championEntryArenaRating: number;
  championEntryRankTier: string;
  replayCount: number;
}

export interface TournamentSeasonLeaderboardEntry {
  rank: number;
  characterId: string;
  characterName: string;
  points: number;
  tournamentsEntered: number;
  championships: number;
  finalistFinishes: number;
  bestPlacement?: number | null;
  latestCompletedAtUtc?: string | null;
  seasonKey: string;
}

export interface TournamentBracket {
  tournamentId: string;
  status: string;
  rounds: TournamentRound[];
}

export interface TournamentParticipant {
  participantId: string;
  characterId: string;
  characterName: string;
  teamId?: string | null;
  isTeamOwner: boolean;
  seed?: number | null;
  entryArenaRating: number;
  entryRankTier: string;
  status: string;
  finalPlacement?: number | null;
}

export interface TournamentTeam {
  teamId: string;
  name: string;
  status: string;
  ownerParticipantId: string;
  ownerName: string;
  memberCount: number;
  missingParticipantCount: number;
  seed?: number | null;
  finalPlacement?: number | null;
  isOpen: boolean;
  isPlayerTeam: boolean;
  isPlayerOwner: boolean;
  members: TournamentParticipant[];
  applications: TournamentTeamApplication[];
  invites: TournamentTeamInvite[];
}

export interface TournamentTeamApplication {
  applicationId: string;
  applicantParticipantId: string;
  applicantCharacterId: string;
  applicantName: string;
  status: string;
  createdAtUtc: string;
}

export interface TournamentTeamInvite {
  inviteId: string;
  invitedParticipantId: string;
  invitedCharacterId: string;
  invitedName: string;
  status: string;
  createdAtUtc: string;
}

export interface TournamentRound {
  id: string;
  roundNumber: number;
  name: string;
  status: string;
  startsAtUtc: string;
  resolvedAtUtc?: string | null;
  matches: TournamentMatch[];
}

export interface TournamentMatch {
  id: string;
  roundNumber: number;
  matchNumber: number;
  status: string;
  outcome: string;
  playerOne?: TournamentTeam | null;
  playerTwo?: TournamentTeam | null;
  winnerTeamId?: string | null;
  combatSessionId?: string | null;
  battleHistoryId?: string | null;
  scheduledAtUtc?: string | null;
  playbackStartedAtUtc?: string | null;
  playbackEndsAtUtc?: string | null;
  hasPlayback: boolean;
}

export interface TournamentPlaybackManifest {
  tournamentId: string;
  matchId: string;
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  overtimeStartsAtTick: number;
  overtimeDurationTicks: number;
  overtimePowerIncreaseIntervalTicks: number;
  overtimePowerIncreasePercent: number;
  frameCount: number;
  playbackStartedAtUtc: string;
  playbackEndsAtUtc: string;
  serverNowUtc: string;
  currentSequence: number;
  isCompleted: boolean;
  bundleETag: string;
}

export interface TournamentPlaybackBundle {
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  entities: TournamentPlaybackEntity[];
  abilities: TournamentPlaybackAbility[];
  frames: TournamentPlaybackFrame[];
}

export interface TournamentPlaybackEntity {
  index: number;
  id: string;
  name: string;
  imagePath: string;
  isFriendly: boolean;
  maxHealth: number;
  level: number;
}

export interface TournamentPlaybackAbility {
  index: number;
  entityIndex: number;
  name: string;
}

export interface TournamentPlaybackFrame {
  sequence: number;
  tick: number;
  isKeyframe?: boolean;
  entityStates: TournamentPlaybackEntityState[];
  entityTotals: TournamentPlaybackEntityTotals[];
  abilityTotals: TournamentPlaybackAbilityTotals[];
  isFinal: boolean;
  outcome?: BattleOutcome | null;
}

export interface TournamentPlaybackEntityState {
  entityIndex: number;
  health: number;
  barrier: number;
}

export interface TournamentPlaybackEntityTotals {
  entityIndex: number;
  damageDone: number;
  damageTaken: number;
  healingDone: number;
  healingReceived: number;
  healthRegenerated: number;
  barrierGenerated: number;
  damageBlocked: number;
  threatGenerated?: number;
}

export interface TournamentPlaybackAbilityTotals {
  abilityIndex: number;
  uses: number;
  totalDamage: number;
  totalHealing: number;
  totalBarrier: number;
  totalThreat?: number;
  damageByType?: AbilityDamageTypeStats[];
}

export interface TournamentCombatFrame {
  sequence: number;
  tick: number;
  friendly: SimpleCombatEntityDto[];
  hostile: SimpleCombatEntityDto[];
  entityStats: EntityStats[];
  events: never[];
  isFinal: boolean;
  outcome?: BattleOutcome | null;
}

export interface RegisterTournamentResponse {
  registered: boolean;
  participantId: string;
  snapshotId: string;
  entryArenaRating: number;
  entryRankTier: string;
  message: string;
}

export interface StartDevelopmentTournamentResponse {
  started: boolean;
  tournamentId?: string | null;
  registeredParticipantCount: number;
  teamCount: number;
}

export interface WithdrawTournamentResponse {
  withdrawn: boolean;
}

export interface CreateTournamentTeamResponse {
  created: boolean;
  teamId: string;
}

export interface TournamentTeamActionResponse {
  succeeded: boolean;
}

export interface TournamentRewardGrant {
  id: string;
  tournamentId: string;
  tournamentName: string;
  rewardKey: string;
  placement?: number | null;
  arenaGlory: number;
  cinders: number;
  soulstones: number;
  temperedScrap?: number;
  blueprintSelectionBoxes: number;
  sigilFragments: number;
  status: string;
  createdAtUtc: string;
  claimedAtUtc?: string | null;
}

export interface TournamentRewardTier {
  key: string;
  maxPlacement?: number | null;
  arenaGlory: number;
  cinders: number;
  soulstones: number;
  temperedScrap?: number;
  blueprintSelectionBoxes: number;
  sigilFragments: number;
}

export interface ClaimTournamentRewardsResponse {
  claimed: boolean;
  arenaGlory: number;
  cinders: number;
  soulstones: number;
  sigilFragments: number;
  temperedScrap?: number;
  blueprintSelectionBoxes: number;
  inventoryGrantId?: string | null;
  inventoryRewards: InventoryItem[];
}
