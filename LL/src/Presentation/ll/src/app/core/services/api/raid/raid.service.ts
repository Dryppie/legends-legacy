import { Injectable, computed, inject, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import {
  AbilityDamageTypeStats,
  BattleOutcome,
} from '../../../../shared/models/Dtos/combatResultDto';
import { ApiService, VersionedMutationResult } from '../api.service';

export type RaidRunStatus =
  | 'Mustering'
  | 'Resolving'
  | 'Playback'
  | 'Resolved'
  | 'Settled'
  | 'Cancelled'
  | 'Expired';
export type RaidOutcome = 'Repelled' | 'Wounded' | 'Broken' | 'Slain';
export type RaidRewardKind = 'WeeklyBase' | 'WeeklyUpgrade' | 'Repeat';
export type RaidLane = 'Rearguard' | 'Vanguard' | 'MainGuard' | 'FinalAssault';

export interface RaidRecommendedWingPower {
  rearguard: number;
  vanguard: number;
  mainGuard: number;
}

export interface RaidBossTierSummary {
  tier: number;
  laneSlots: number;
  minimumRoster: number;
  signupWindowHours: number;
  recommendedWingPower: RaidRecommendedWingPower;
}

export interface RaidBossSummary {
  id: string;
  name: string;
  region: number;
  regions: number[];
  levelRequirement: number;
  imagePath: string;
  isUnlocked: boolean;
  lockReason: string | null;
  openRaidCount: number;
  hasWeeklyRewardThisWeek: boolean;
  activeRaidId: string | null;
  tiers: RaidBossTierSummary[];
  developmentToolsEnabled: boolean;
}

export interface RaidRunSummary {
  id: string;
  raidBossId: string;
  raidBossName: string;
  tier: number;
  leaderCharacterId: string;
  leaderCharacterName: string;
  status: RaidRunStatus;
  signupClosesAt: string;
  signupCount: number;
  maximumRoster: number;
  rearguardCount: number;
  vanguardCount: number;
  mainGuardCount: number;
  canJoin: boolean;
}

export interface RaidHistoryEntry {
  raidRunId: string;
  raidBossId: string;
  raidBossName: string;
  tier: number;
  outcome: RaidOutcome;
  resolvedAt: string;
  trophies: number;
  rewardKind: RaidRewardKind;
  claimedAt: string | null;
  canClaim: boolean;
}

export interface RaidSignup {
  characterId: string;
  characterName: string;
  powerRating: number;
  lane: RaidLane | null;
  wingSlotIndex: number | null;
  signedUpAt: string;
  snapshotRefreshedAt: string | null;
  isLeader: boolean;
  isCurrentCharacter: boolean;
}

export interface RaidJoinRequest {
  characterId: string;
  characterName: string;
  powerRating: number;
  requestedAt: string;
  snapshotRefreshedAt: string | null;
  isCurrentCharacter: boolean;
}

export interface RaidLaneResult {
  lane: RaidLane;
  durationTicks: number;
  battleOutcome: 'Victory' | 'Defeat' | 'Draw';
  totalFriendlyDamage: number;
  survivingHostileHealthFraction: number;
  derivedModifier: number;
  hasPlayback: boolean;
}

export interface RaidBattlePlanLane {
  lane: RaidLane;
  readiness: string;
  successProbability: number;
  successProbabilityLower: number;
  successProbabilityUpper: number;
  averageDurationTicks: number;
  expectedDerivedModifier: number;
  derivedModifierLower: number;
  derivedModifierUpper: number;
}

export interface RaidBattlePlanPreview {
  raidRunId: string;
  generatedAt: string;
  sampleCount: number;
  readiness: string;
  predictedOutcome: RaidOutcome;
  slainProbability: number;
  slainProbabilityLower: number;
  slainProbabilityUpper: number;
  lanes: RaidBattlePlanLane[];
  outcomeCounts: Partial<Record<RaidOutcome, number>>;
}

export interface RaidPlayback {
  raidRunId: string;
  lane: RaidLane;
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  frameCount: number;
  bundleETag: string;
}

export interface RaidPlaybackEntity {
  index: number;
  id: string;
  name: string;
  imagePath: string;
  isFriendly: boolean;
  maxHealth: number;
  level: number;
  partyNumber?: number | null;
}

export interface RaidPlaybackFrame {
  sequence: number;
  tick: number;
  isKeyframe?: boolean;
  entityStates: {
    entityIndex: number;
    health: number;
    barrier: number;
    currentStagger?: number;
    maxStagger?: number;
    isStaggered?: boolean;
    isStaggerRecovering?: boolean;
  }[];
  entityTotals: {
    entityIndex: number;
    damageDone: number;
    damageTaken: number;
    healingDone: number;
    healingReceived: number;
    healthRegenerated: number;
    barrierGenerated: number;
    damageBlocked: number;
    threatGenerated?: number;
    staggerContributed?: number;
    staggerBreaks?: number;
  }[];
  abilityTotals: RaidPlaybackAbilityTotals[];
  isFinal: boolean;
  outcome: BattleOutcome | null;
}

export interface RaidPlaybackBundle {
  schemaVersion: number;
  ticksPerSecond: number;
  ticksPerFrame: number;
  totalTicks: number;
  entities: RaidPlaybackEntity[];
  abilities: RaidPlaybackAbility[];
  frames: RaidPlaybackFrame[];
}

export interface RaidPlaybackAbility {
  index: number;
  entityIndex: number;
  name: string;
}

export interface RaidPlaybackAbilityTotals {
  abilityIndex: number;
  uses: number;
  totalDamage: number;
  totalHealing: number;
  totalBarrier: number;
  damageByType?: AbilityDamageTypeStats[];
  totalThreat?: number;
  totalStagger?: number;
  staggerBreaks?: number;
}

export interface RaidParticipantResult {
  characterId: string;
  lane: RaidLane;
  damageDone: number;
  contributionScore: number;
  contributionRank: number;
}

export interface RaidRun {
  id: string;
  version?: number;
  raidBossId: string;
  raidBossName: string;
  imagePath: string;
  region: number;
  tier: number;
  status: RaidRunStatus;
  leaderCharacterId: string;
  createdAt: string;
  signupClosesAt: string;
  commencedAt: string | null;
  playbackStartedAt: string | null;
  playbackEndsAt: string | null;
  serverNow: string;
  resolvedAt: string | null;
  laneSlots: number;
  minimumRoster: number;
  signups: RaidSignup[];
  joinRequests: RaidJoinRequest[];
  laneResults: RaidLaneResult[];
  participantResults: RaidParticipantResult[];
  outcome: RaidOutcome | null;
  reinforcementPenalty: number | null;
  guardianBreak: number | null;
  signatureDisruption: number | null;
  bossHealthRemainingPercent: number | null;
  canJoin: boolean;
  canLeave: boolean;
  canAssign: boolean;
  canCommence: boolean;
  canRefreshSnapshot: boolean;
  canClaim: boolean;
  rewardKind: RaidRewardKind | null;
  canPreviewBattlePlan: boolean;
  canCancel: boolean;
  canTransferLeadership: boolean;
  developmentToolsEnabled: boolean;
}

export interface RaidReward {
  raidRunId: string;
  trophies: number;
  trophyBalance: number;
  items: { itemId: string; itemName: string; quantity: number }[];
  rewardKind: RaidRewardKind;
  claimedAt: string;
}

export interface RaidTrophyVendorItem {
  id: string;
  name: string;
  description: string;
  category: string;
  trophyCost: number;
  rewardItemId: string;
  rewardQuantity: number;
  weeklyPurchaseLimit: number | null;
  weeklyPurchased: number;
  lifetimePurchaseLimit: number | null;
  lifetimePurchased: number;
  requiredTier: number;
  isUnlocked: boolean;
  canPurchase: boolean;
}

export interface RaidTrophyVendor {
  raidBossId: string;
  trophyBalance: number;
  items: RaidTrophyVendorItem[];
}

export interface RaidTrophyPurchase {
  raidBossId: string;
  vendorItemId: string;
  rewardItemId: string;
  rewardQuantity: number;
  trophiesSpent: number;
  trophyBalance: number;
  purchasedAt: string;
}

@Injectable({ providedIn: 'root' })
export class RaidService {
  private readonly api = inject(ApiService);
  private readonly _activeRaid = signal<RaidRun | null>(null);
  private readonly _activeRaidId = signal<string | null>(null);
  private readonly _activeRaidChatId = signal<string | null>(null);
  private raidQueryEpoch = 0;
  readonly activeRaid = this._activeRaid.asReadonly();
  readonly activeRaidId = this._activeRaidId.asReadonly();
  readonly activeRaidChatId = this._activeRaidChatId.asReadonly();
  readonly hasActiveRaid = computed(() => this._activeRaid() !== null);

  getRaidBosses(region?: number): Observable<RaidBossSummary[]> {
    return this.api.get(`raids/bosses${region ? `?region=${region}` : ''}`);
  }

  getOpenRaids(raidBossId: string): Observable<RaidRunSummary[]> {
    return this.api.get(`raids/bosses/${encodeURIComponent(raidBossId)}/open`);
  }

  getHistory(raidBossId?: string, take = 20): Observable<RaidHistoryEntry[]> {
    const bossFilter = raidBossId
      ? `raidBossId=${encodeURIComponent(raidBossId)}&`
      : '';
    return this.api.get(`raids/history?${bossFilter}take=${take}`);
  }

  getRaid(raidRunId: string): Observable<RaidRun | null> {
    const queryEpoch = ++this.raidQueryEpoch;
    return this.api.get(`raids/${raidRunId}`).pipe(
      tap((raid) => {
        if (queryEpoch === this.raidQueryEpoch) {
          this.trackRaid(raid as RaidRun | null, false);
        }
      }),
    );
  }

  getActiveRaid(): Observable<RaidRun | null> {
    const queryEpoch = ++this.raidQueryEpoch;
    return this.api.get('raids/active').pipe(
      tap((raid) => {
        if (queryEpoch !== this.raidQueryEpoch) return;
        if (raid) this.trackRaid(raid as RaidRun, false);
        else this.clearActiveRaid(undefined, false);
      }),
    );
  }

  create(raidBossId: string, plusLevel: number): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        'raids/create',
        { raidBossId, plusLevel },
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  createDevelopment(
    raidBossId: string,
    plusLevel: number,
  ): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/bosses/${encodeURIComponent(raidBossId)}/development/create`,
        { plusLevel },
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  fillDevelopmentTeam(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/development/fill`,
        {},
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  join(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/join`,
        {},
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  approveSignup(raidRunId: string, characterId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/signups/approve`,
        { characterId },
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  removeSignup(raidRunId: string, characterId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/signups/remove`,
        { characterId },
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  leave(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/leave`,
        {},
      ),
    ).pipe(tap(() => this.clearActiveRaid(raidRunId)));
  }

  cancel(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/cancel`,
        {},
      ),
    ).pipe(tap(() => this.clearActiveRaid(raidRunId)));
  }

  transferLeadership(
    raidRunId: string,
    characterId: string,
  ): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/transfer-leadership`,
        { characterId },
      ),
    );
  }

  refreshLoadout(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/loadout`,
        {},
      ),
    );
  }

  assign(
    raidRunId: string,
    characterId: string,
    lane: RaidLane,
    slotIndex: number,
  ): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/assign`,
        { characterId, lane, slotIndex },
      ),
    );
  }

  updateParties(
    raidRunId: string,
    assignments: ReadonlyArray<{
      characterId: string;
      lane: RaidLane | null;
      wingSlotIndex: number | null;
    }>,
  ): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.putVersioned<RaidRun>(
        `raids/${raidRunId}/parties`,
        { assignments },
      ),
    );
  }

  commence(raidRunId: string): Observable<RaidRun> {
    return this.unwrapRaidMutation(
      this.api.postVersioned<RaidRun>(
        `raids/${raidRunId}/commence`,
        {},
      ),
    ).pipe(tap((raid) => this.trackRaid(raid)));
  }

  previewBattlePlan(raidRunId: string): Observable<RaidBattlePlanPreview> {
    return this.api.post(`raids/${raidRunId}/battle-plan`);
  }

  getPlayback(raidRunId: string, lane: RaidLane): Observable<RaidPlayback> {
    return this.api.get(`raids/${raidRunId}/lanes/${lane}/playback`);
  }

  getPlaybackBundle(
    raidRunId: string,
    lane: RaidLane,
  ): Observable<RaidPlaybackBundle> {
    return this.api.get(`raids/${raidRunId}/lanes/${lane}/playback/bundle`);
  }

  claim(raidRunId: string): Observable<RaidReward> {
    return this.api.post(`raids/${raidRunId}/claim`);
  }

  getTrophyVendor(raidBossId: string): Observable<RaidTrophyVendor | null> {
    return this.api.get(
      `raids/bosses/${encodeURIComponent(raidBossId)}/vendor`,
    );
  }

  purchaseTrophyVendorItem(
    raidBossId: string,
    itemId: string,
    quantity = 1,
  ): Observable<RaidTrophyPurchase> {
    return this.api.post(
      `raids/bosses/${encodeURIComponent(raidBossId)}/vendor/purchase`,
      { itemId, quantity },
    );
  }

  clearActiveRaid(raidRunId?: string, invalidateQueries = true): void {
    if (invalidateQueries) this.raidQueryEpoch += 1;
    if (!raidRunId || this._activeRaidId() === raidRunId) {
      this._activeRaid.set(null);
      this._activeRaidId.set(null);
    }
    if (!raidRunId || this._activeRaidChatId() === raidRunId) {
      this._activeRaidChatId.set(null);
    }
  }

  private unwrapRaidMutation(
    request: Observable<VersionedMutationResult<RaidRun>>,
  ): Observable<RaidRun> {
    return request.pipe(map((result) => result.data));
  }

  private trackRaid(
    raid: RaidRun | null | undefined,
    invalidateQueries = true,
  ): void {
    if (invalidateQueries) this.raidQueryEpoch += 1;
    if (!raid?.id) return;

    const isActive =
      raid.status === 'Mustering' ||
      raid.status === 'Resolving' ||
      raid.status === 'Playback';
    const isApprovedMember = raid.signups?.some(
      (signup) => signup.isCurrentCharacter,
    );
    const hasPendingRequest = raid.joinRequests?.some(
      (request) => request.isCurrentCharacter,
    );
    if (isActive && (isApprovedMember || hasPendingRequest)) {
      this._activeRaid.set(raid);
      this._activeRaidId.set(raid.id);
    } else if (this._activeRaidId() === raid.id) {
      this._activeRaid.set(null);
      this._activeRaidId.set(null);
    }

    if (isActive && isApprovedMember) {
      this._activeRaidChatId.set(raid.id);
    } else if (this._activeRaidChatId() === raid.id) {
      this._activeRaidChatId.set(null);
    }
  }
}
