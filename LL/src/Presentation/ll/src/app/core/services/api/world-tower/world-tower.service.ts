import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BattleOutcome,
  CombatResultDto,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../../shared/models/Dtos/combatResultDto';
import { ApiService } from '../api.service';

export type TowerFloorType = 'Standard' | 'Warden' | 'Sovereign';
export type TowerFloorStateType =
  | 'Locked'
  | 'Sealed'
  | 'Scouting'
  | 'Rallying'
  | 'Cleared';
export type TowerRallyMode = 'FirstClear' | 'Echo';
export type TowerRallyStatus =
  | 'Recruiting'
  | 'Ready'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';
export type TowerRallyApplicationStatus =
  | 'Pending'
  | 'Accepted'
  | 'Declined'
  | 'Withdrawn';
export type TowerAttemptStatus =
  | 'Started'
  | 'Playback'
  | 'Succeeded'
  | 'Failed'
  | 'Errored';
export type TowerContributionKind =
  | 'Research'
  | 'SupplyWeapons'
  | 'InscribeWards'
  | 'ScoutWeakPoints';

export interface TowerOverview {
  serverId: string;
  highestUnlockedFloor: number;
  highestClearedFloor: number;
  echoModeUnlocked: boolean;
  towerTokens: number;
  currentFloor: TowerFloorSummary | null;
  floors: TowerFloorSummary[];
  activeRallies: TowerRallySummary[];
  recentClears: TowerHallOfFameEntry[];
}

export interface TowerFloorSummary {
  floorNumber: number;
  name: string;
  type: TowerFloorType;
  state: TowerFloorStateType;
  requiredSlots: number;
  recommendedPowerRating: number;
  scoutingProgress: number;
  guardianName: string;
}

export interface TowerFloorDetail {
  floorNumber: number;
  name: string;
  type: TowerFloorType;
  state: TowerFloorStateType;
  requiredSlots: number;
  recommendedPowerRating: number;
  scoutingProgress: number;
  weeklyResearchContribution: number;
  weeklyResearchCap: number;
  canCreateRally: boolean;
  currentCharacterRallyId: string | null;
  canCreateFirstClearRally: boolean;
  echoAvailable: boolean;
  guardian: TowerGuardianInfo;
  preparation: TowerPreparationSummary;
  activeRallies: TowerRallySummary[];
  unlocks: TowerUnlock[];
  firstClearTowerTokens: number;
  echoTowerTokens: number;
  echoRewardClaimedThisWeek: boolean;
}

export interface TowerGuardianInfo {
  name: string;
  tags: string[];
  knownReveals: TowerScoutingReveal[];
}

export interface TowerUnlock {
  key: string;
  description: string;
}

export interface TowerScoutingReveal {
  threshold: number;
  title: string;
  description: string;
  kind: 'Active' | 'Passive';
  cooldownSeconds: number | null;
}

export interface TowerPreparationSummary {
  supplyWeaponsPercent: number;
  inscribeWardsPercent: number;
  scoutWeakPointsPercent: number;
  weeklyCharacterContribution: number;
  weeklyCharacterCap: number;
  maximumEffectPercent: number;
}

export interface TowerRallySummary {
  id: string;
  floorNumber: number;
  mode: TowerRallyMode;
  leaderCharacterName: string;
  status: TowerRallyStatus;
  participantCount: number;
  requiredSlots: number;
  pendingApplicationCount: number;
  createdAt: string;
}

export interface TowerRally {
  id: string;
  floorNumber: number;
  guardianName: string;
  mode: TowerRallyMode;
  status: TowerRallyStatus;
  createdByCharacterId: string;
  requiredSlots: number;
  createdAt: string;
  participants: TowerRallyParticipant[];
  applications: TowerRallyApplication[];
  readiness: TowerRosterReadiness;
  canApply: boolean;
  canManageApplications: boolean;
  canLeave: boolean;
  canStart: boolean;
  developmentToolsEnabled: boolean;
  attempt: TowerAttemptSummary | null;
}

export interface TowerRallyApplication {
  id: string;
  characterId: string;
  characterName: string;
  guildName: string | null;
  powerRating: number;
  status: TowerRallyApplicationStatus;
  appliedAt: string;
  isCurrentCharacter: boolean;
}

export interface TowerRallyParticipant {
  characterId: string;
  characterName: string;
  guildName: string | null;
  powerRating: number;
  joinedAt: string;
  isLeader: boolean;
  isCurrentCharacter: boolean;
}

export interface TowerRosterReadiness {
  rating: string;
  averagePowerRating: number;
  recommendedPowerRating: number;
  warnings: string[];
}

export interface TowerAttemptSummary {
  id: string;
  status: TowerAttemptStatus;
  succeeded: boolean;
  fightDurationSeconds: number | null;
  failureReason: string | null;
  canViewCombatResult: boolean;
  playback: TowerCombatPlayback | null;
  battleReport: TowerBattleReport | null;
}

export interface TowerAttemptResult {
  attemptId: string;
  floorNumber: number;
  guardianName: string;
  status: TowerAttemptStatus;
  playback: TowerCombatPlayback | null;
}

export interface TowerCombatPlayback {
  attemptId: string;
  rallyId: string;
  playbackStartedAt: string;
  playbackEndsAt: string;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  frameCount: number;
  currentSequence: number;
  currentFrame: TowerCombatFrame;
  isCompleted: boolean;
}

export interface TowerCombatFrame {
  sequence: number;
  tick: number;
  friendly: SimpleCombatEntityDto[];
  hostile: SimpleCombatEntityDto[];
  entityStats: EntityStats[];
  events: TowerCombatEvent[];
  isFinal: boolean;
  outcome: BattleOutcome | null;
}

export interface TowerCombatFrameBatch {
  attemptId: string;
  afterSequence: number;
  currentSequence: number;
  hasMore: boolean;
  frames: TowerCombatFrame[];
}

export interface TowerCombatEvent {
  source: string;
  statsSource: string;
  countsAsActivation: boolean;
  timestamp: number;
  actorId: string;
  targetId: string;
  eventType: string;
  magnitude: number;
  details: string;
}

export interface TowerBattleReport {
  floorNumber: number;
  guardianName: string;
  succeeded: boolean;
  mainFailureReason: string | null;
  fightDurationSeconds: number;
  guardianHealthRemainingPercent: number;
  participants: TowerParticipantCombatSummary[];
  rosterSummary: TowerRosterReadiness;
}

export interface TowerParticipantCombatSummary {
  characterId: string;
  characterName: string;
  damageDone: number;
  damageTaken: number;
  healingDone: number;
  survived: boolean;
}

export interface TowerHallOfFameEntry {
  floorNumber: number;
  floorName: string;
  guardianName: string;
  attemptId: string;
  clearedAt: string;
  attemptNumber: number;
  fightDurationSeconds: number;
  participants: TowerHallOfFameParticipant[];
}

export interface TowerHallOfFameParticipant {
  characterId: string;
  characterName: string;
  guildName: string | null;
  powerRating: number;
}

@Injectable({ providedIn: 'root' })
export class WorldTowerService {
  private readonly api = inject(ApiService);

  getOverview(): Observable<TowerOverview> {
    return this.api.get('world-tower');
  }

  getFloor(floorNumber: number): Observable<TowerFloorDetail> {
    return this.api.get(`world-tower/floors/${floorNumber}`);
  }

  getRally(rallyId: string): Observable<TowerRally | null> {
    return this.api.get(`world-tower/rallies/${rallyId}`);
  }

  getAttemptReport(attemptId: string): Observable<TowerBattleReport> {
    return this.api.get(`world-tower/attempts/${attemptId}/report`);
  }

  getAttemptCombatResult(attemptId: string): Observable<CombatResultDto> {
    return this.api.get(`world-tower/attempts/${attemptId}/combat-result`);
  }

  getAttemptPlayback(attemptId: string): Observable<TowerCombatPlayback> {
    return this.api.get(`world-tower/attempts/${attemptId}/playback`);
  }

  getAttemptPlaybackFrames(
    attemptId: string,
    afterSequence: number,
  ): Observable<TowerCombatFrameBatch> {
    return this.api.get(
      `world-tower/attempts/${attemptId}/playback/frames?after=${afterSequence}`,
    );
  }

  getHallOfFame(): Observable<TowerHallOfFameEntry[]> {
    return this.api.get('world-tower/hall-of-fame');
  }

  createRally(
    floorNumber: number,
    mode: TowerRallyMode,
  ): Observable<TowerRally> {
    return this.api.post('world-tower/rallies', { floorNumber, mode });
  }

  applyToRally(rallyId: string): Observable<TowerRally> {
    return this.api.post(`world-tower/rallies/${rallyId}/applications`);
  }

  acceptApplication(
    rallyId: string,
    applicationId: string,
  ): Observable<TowerRally> {
    return this.api.post(
      `world-tower/rallies/${rallyId}/applications/${applicationId}/accept`,
    );
  }

  declineApplication(
    rallyId: string,
    applicationId: string,
  ): Observable<TowerRally> {
    return this.api.post(
      `world-tower/rallies/${rallyId}/applications/${applicationId}/decline`,
    );
  }

  leaveRally(rallyId: string): Observable<TowerRally> {
    return this.api.post(`world-tower/rallies/${rallyId}/leave`);
  }

  fillDevelopmentRoster(rallyId: string): Observable<TowerRally> {
    return this.api.post(
      `world-tower/rallies/${rallyId}/development/fill-roster`,
    );
  }

  startRally(rallyId: string): Observable<TowerAttemptResult> {
    return this.api.post(`world-tower/rallies/${rallyId}/start`);
  }

  contribute(
    floorNumber: number,
    kind: TowerContributionKind,
    amount = 1,
  ): Observable<TowerFloorDetail> {
    return this.api.post(`world-tower/floors/${floorNumber}/contributions`, {
      kind,
      amount,
    });
  }
}
