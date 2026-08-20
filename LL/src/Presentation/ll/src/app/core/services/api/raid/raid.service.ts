import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  AbilityDamageTypeStats,
  BattleOutcome,
} from '../../../../shared/models/Dtos/combatResultDto';
import { ApiService } from '../api.service';

export type RaidRunStatus =
  | 'Mustering'
  | 'Resolving'
  | 'Playback'
  | 'Resolved'
  | 'Settled'
  | 'Cancelled'
  | 'Expired';
export type RaidOutcome = 'Repelled' | 'Wounded' | 'Broken' | 'Slain';
export type RaidLane = 'Vanguard' | 'Flank' | 'Ward';

export interface RaidRecommendedWingPower {
  vanguard: number;
  flank: number;
  ward: number;
  vanguardLower: number;
  vanguardUpper: number;
  flankLower: number;
  flankUpper: number;
  wardLower: number;
  wardUpper: number;
  confidence: 'Low' | 'Medium' | 'High';
  isCalibrated: boolean;
}

export interface RaidBossTierSummary {
  tier: number;
  laneSlots: number;
  minimumRoster: number;
  signupWindowHours: number;
  raidSealItemId: string;
  raidSealFragmentItemId: string;
  raidSealFragmentCost: number;
  ownedRaidSealFragments: number;
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
  ownedRaidSealCount: number;
  rewardReducedThisWeek: boolean;
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
  vanguardCount: number;
  flankCount: number;
  wardCount: number;
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
  wasReduced: boolean;
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
  entityStates: { entityIndex: number; health: number; barrier: number }[];
  entityTotals: {
    entityIndex: number;
    damageDone: number;
    damageTaken: number;
    healingDone: number;
    healingReceived: number;
    healthRegenerated: number;
    barrierGenerated: number;
    damageBlocked: number;
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
}

export interface RaidParticipantResult {
  characterId: string;
  lane: RaidLane;
  damageDone: number;
  contributionScore: number;
  payoutMultiplier: number;
  contributionRank: number;
}

export interface RaidRun {
  id: string;
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
  laneResults: RaidLaneResult[];
  participantResults: RaidParticipantResult[];
  outcome: RaidOutcome | null;
  reinforcementPenalty: number | null;
  wardBreak: number | null;
  bossHealthRemainingPercent: number | null;
  canJoin: boolean;
  canLeave: boolean;
  canAssign: boolean;
  canCommence: boolean;
  canRefreshSnapshot: boolean;
  canClaim: boolean;
  rewardWasReduced: boolean;
  canPreviewBattlePlan: boolean;
  canCancel: boolean;
  canTransferLeadership: boolean;
  developmentToolsEnabled: boolean;
}

export interface RaidReward {
  raidRunId: string;
  trophies: number;
  trophyBalance: number;
  items: { itemId: string; quantity: number }[];
  wasReduced: boolean;
  claimedAt: string;
}

export interface RaidSealAssembly {
  raidBossId: string;
  tier: number;
  raidSealItemId: string;
  ownedRaidSealCount: number;
  fragmentsRemaining: number;
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
  private readonly _activeRaidId = signal<string | null>(null);
  readonly activeRaidId = this._activeRaidId.asReadonly();

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
    return this.api
      .get(`raids/${raidRunId}`)
      .pipe(tap((raid) => this.trackRaid(raid as RaidRun | null)));
  }

  getActiveRaid(): Observable<RaidRun | null> {
    return this.api.get('raids/active').pipe(
      tap((raid) => {
        if (raid) this.trackRaid(raid as RaidRun);
        else this._activeRaidId.set(null);
      }),
    );
  }

  create(raidBossId: string, tier: number): Observable<RaidRun> {
    return this.api
      .post('raids/create', { raidBossId, tier })
      .pipe(tap((raid) => this.trackRaid(raid as RaidRun)));
  }

  createDevelopment(raidBossId: string, tier: number): Observable<RaidRun> {
    return this.api
      .post(
        `raids/bosses/${encodeURIComponent(raidBossId)}/development/create`,
        { tier },
      )
      .pipe(tap((raid) => this.trackRaid(raid as RaidRun)));
  }

  join(raidRunId: string): Observable<RaidRun> {
    return this.api
      .post(`raids/${raidRunId}/join`)
      .pipe(tap((raid) => this.trackRaid(raid as RaidRun)));
  }

  leave(raidRunId: string): Observable<RaidRun> {
    return this.api
      .post(`raids/${raidRunId}/leave`)
      .pipe(tap(() => this.clearActiveRaid(raidRunId)));
  }

  cancel(raidRunId: string): Observable<RaidRun> {
    return this.api
      .post(`raids/${raidRunId}/cancel`)
      .pipe(tap(() => this.clearActiveRaid(raidRunId)));
  }

  transferLeadership(
    raidRunId: string,
    characterId: string,
  ): Observable<RaidRun> {
    return this.api.post(`raids/${raidRunId}/transfer-leadership`, {
      characterId,
    });
  }

  refreshLoadout(raidRunId: string): Observable<RaidRun> {
    return this.api.post(`raids/${raidRunId}/loadout`);
  }

  assign(
    raidRunId: string,
    characterId: string,
    lane: RaidLane,
    slotIndex: number,
  ): Observable<RaidRun> {
    return this.api.post(`raids/${raidRunId}/assign`, {
      characterId,
      lane,
      slotIndex,
    });
  }

  updateParties(
    raidRunId: string,
    assignments: ReadonlyArray<{
      characterId: string;
      lane: RaidLane | null;
      wingSlotIndex: number | null;
    }>,
  ): Observable<RaidRun> {
    return this.api.put(`raids/${raidRunId}/parties`, { assignments });
  }

  fillDevelopmentRoster(raidRunId: string): Observable<RaidRun> {
    return this.api.post(`raids/${raidRunId}/development/fill-roster`);
  }

  commence(raidRunId: string): Observable<RaidRun> {
    return this.api
      .post(`raids/${raidRunId}/commence`)
      .pipe(tap((raid) => this.trackRaid(raid as RaidRun)));
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

  assembleRaidSeal(
    raidBossId: string,
    tier: number,
  ): Observable<RaidSealAssembly> {
    return this.api.post(
      `raids/bosses/${encodeURIComponent(raidBossId)}/assemble-raid-seal`,
      { tier },
    );
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

  clearActiveRaid(raidRunId?: string): void {
    if (!raidRunId || this._activeRaidId() === raidRunId) {
      this._activeRaidId.set(null);
    }
  }

  private trackRaid(raid: RaidRun | null | undefined): void {
    if (!raid?.id) return;

    const isActive =
      raid.status === 'Mustering' ||
      raid.status === 'Resolving' ||
      raid.status === 'Playback';
    const isMember = raid.signups?.some((signup) => signup.isCurrentCharacter);
    if (isActive && isMember) {
      this._activeRaidId.set(raid.id);
    } else if (this._activeRaidId() === raid.id) {
      this._activeRaidId.set(null);
    }
  }
}
