import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../../shared/models/Dtos/combatResultDto';
import { ApiService } from '../api.service';

export type RegionBossEventStatus =
  | 'Scheduled'
  | 'SignupOpen'
  | 'Matching'
  | 'Resolving'
  | 'Playback'
  | 'Settled'
  | 'Cancelled';
export type RegionBossRunStatus =
  | 'Queued'
  | 'Resolving'
  | 'Ready'
  | 'Settled'
  | 'Errored';

export interface RegionBossParticipantResult {
  damageDone: number;
  damageTaken: number;
  healingDone: number;
  healingReceived: number;
  barrierGenerated: number;
  damagePrevented: number;
  threatGenerated: number;
  deaths: number;
  revivals: number;
  downedTicks: number;
}

export interface RegionBossPartyMember {
  characterId: string;
  characterName: string;
  partySlot: number;
  powerRating: number;
  result: RegionBossParticipantResult | null;
}

export interface RegionBossRun {
  runId: string;
  partyNumber: number;
  status: RegionBossRunStatus;
  highestLevelDefeated: number;
  currentBossLevel: number;
  currentBossHealthRemaining: number;
  currentBossMaxHealth: number;
  currentBossProgressBasisPoints: number;
  durationTicks: number;
  furyStacks: number;
  terminationReason: string | null;
  members: RegionBossPartyMember[];
  hasPlayback: boolean;
}

export interface RegionBossReward {
  grantId: string;
  rewardKey: string;
  milestoneLevel: number;
  status: 'Unclaimed' | 'Claimed';
  cinders: number;
  soulstones: number;
  claimedAtUtc: string | null;
}

export interface RegionBossStatus {
  eventId: string;
  definitionId: string;
  name: string;
  imagePath: string;
  regionId: number;
  status: RegionBossEventStatus;
  signupStartsAtUtc: string;
  signupClosesAtUtc: string;
  encounterStartsAtUtc: string;
  playbackStartsAtUtc: string | null;
  playbackEndsAtUtc: string | null;
  serverNowUtc: string;
  isUnlocked: boolean;
  lockReason: string | null;
  isSignedUp: boolean;
  signupCount: number;
  run: RegionBossRun | null;
  rewards: RegionBossReward[];
}

export interface RegionBossPlaybackFrame {
  sequence: number;
  tick: number;
  friendly: SimpleCombatEntityDto[];
  hostile: SimpleCombatEntityDto[];
  entityStats: EntityStats[];
  events: unknown[];
  isFinal: boolean;
  context: {
    waveNumber: number;
    furyStacks: number;
    downed: RegionBossDownedState[];
  } | null;
}

export interface RegionBossDownedState {
  entityId: string;
  deaths: number;
  reviveAtTick: number;
  remainingTicks: number;
}

export interface RegionBossPlaybackBundle {
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  highestLevelDefeated: number;
  currentBossLevel: number;
  terminationReason: string;
  frames: RegionBossPlaybackFrame[];
}

@Injectable({ providedIn: 'root' })
export class RegionBossService {
  private readonly api = inject(ApiService);

  getStatus(regionId?: number): Observable<RegionBossStatus[]> {
    const suffix = regionId ? `?regionId=${regionId}` : '';
    return this.api.get(`region-bosses${suffix}`);
  }

  signup(eventId: string): Observable<RegionBossStatus> {
    return this.api.post(`region-bosses/events/${eventId}/signup`, {});
  }

  withdraw(eventId: string): Observable<RegionBossStatus> {
    return this.api.delete(`region-bosses/events/${eventId}/signup`);
  }

  claim(grantId: string): Observable<unknown> {
    return this.api.post(`region-bosses/rewards/${grantId}/claim`, {});
  }

  spawnDevelopment(
    regionId: number,
    additionalSignupCount = 24,
  ): Observable<RegionBossStatus> {
    return this.api.post('region-bosses/development/spawn', {
      regionId,
      additionalSignupCount,
    });
  }

  getPlaybackBundle(runId: string): Observable<RegionBossPlaybackBundle> {
    return this.api.get(`region-bosses/runs/${runId}/playback/bundle`);
  }
}
