import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AbilityDamageTypeStats,
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
  tags: string[];
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
  startedAt: string | null;
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
  canUpdateLoadout: boolean;
  canTransferLeadership: boolean;
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
  currentFrame: TowerCombatFrame | null;
  isCompleted: boolean;
  schemaVersion: number;
  serverNow: string | null;
  bundleETag: string | null;
}

export interface TowerPlaybackBundle {
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  entities: TowerPlaybackEntity[];
  abilities: TowerPlaybackAbility[];
  frames: TowerPlaybackBundleFrame[];
}

export interface TowerPlaybackEntity {
  index: number;
  id: string;
  name: string;
  imagePath: string;
  isFriendly: boolean;
  maxHealth: number;
  level: number;
}

export interface TowerPlaybackAbility {
  index: number;
  entityIndex: number;
  name: string;
}

export interface TowerPlaybackBundleFrame {
  sequence: number;
  tick: number;
  entityStates: TowerPlaybackEntityState[];
  entityTotals: TowerPlaybackEntityTotals[];
  abilityTotals: TowerPlaybackAbilityTotals[];
  isFinal: boolean;
  outcome: BattleOutcome | null;
}

export interface TowerPlaybackEntityState {
  entityIndex: number;
  health: number;
  barrier: number;
}

export interface TowerPlaybackEntityTotals {
  entityIndex: number;
  damageDone: number;
  damageTaken: number;
  healingDone: number;
  healingReceived: number;
  healthRegenerated: number;
  barrierGenerated: number;
  damageBlocked: number;
}

export interface TowerPlaybackAbilityTotals {
  abilityIndex: number;
  uses: number;
  totalDamage: number;
  totalHealing: number;
  totalBarrier: number;
  damageByType?: AbilityDamageTypeStats[];
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

export interface TowerPersonalExpedition {
  rallyId: string;
  attemptId: string;
  floorNumber: number;
  floorName: string;
  guardianName: string;
  mode: TowerRallyMode;
  status: TowerAttemptStatus;
  attemptNumber: number;
  startedAt: string;
  completedAt: string | null;
  fightDurationSeconds: number | null;
  participants: TowerHallOfFameParticipant[];
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

  getAttemptPlaybackBundle(attemptId: string): Observable<TowerPlaybackBundle> {
    return this.api.get(`world-tower/attempts/${attemptId}/playback/bundle`);
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

  getPersonalExpeditions(): Observable<TowerPersonalExpedition[]> {
    return this.api.get('world-tower/personal-expeditions');
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

  updateRallyLoadout(rallyId: string): Observable<TowerRally> {
    return this.api.post(`world-tower/rallies/${rallyId}/loadout`);
  }

  transferRallyLeadership(
    rallyId: string,
    characterId: string,
  ): Observable<TowerRally> {
    return this.api.post(`world-tower/rallies/${rallyId}/leader`, {
      characterId,
    });
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
