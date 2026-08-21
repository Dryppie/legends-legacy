import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, VersionedMutationResult } from '../api.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface PropheciesOverviewDto {
  serverTime: string;
  dailyRerollsRemaining: number;
  dailyRerollsUsed: number;
  dailyRerollLimit: number;
  nextDailyRerollCost?: number | null;
  fateEcho: number;
  dailyProphecies: ProphecyInstanceDto[];
  activeDailyProphecy?: ProphecyInstanceDto | null;
  greaterProphecy: ProphecyInstanceDto;
  weeklyRevelation: WeeklyRevelationProgressDto;
  caches: ProphecyCacheInventoryDto[];
}

export interface ProphecyCacheInventoryDto {
  itemId: string;
  title: string;
  description: string;
  quantity: number;
  possibleRewards: string[];
}

export interface ProphecyInstanceDto {
  id: string;
  definitionId: string;
  title: string;
  flavorText: string;
  objectiveText: string;
  scope: string;
  slotType: string;
  status: string;
  category: string;
  difficulty: string;
  objectiveType: string;
  guidance: ProphecyGuidanceDto;
  periodStart: string;
  periodEnd: string;
  generatedAt: string;
  acceptedAt?: string | null;
  completedAt?: string | null;
  claimedAt?: string | null;
  targetValue: number;
  currentValue: number;
  reward: ProphecyRewardSnapshotDto;
}

export type ProphecyGuidanceDestination =
  | 'WorldCombat'
  | 'Dungeons'
  | 'Essences'
  | 'SoulArchive'
  | 'Gathering'
  | 'Crafting';

export interface ProphecyGuidanceDto {
  destination: ProphecyGuidanceDestination;
  actionLabel: string;
  hint: string;
}

export interface ProphecyRewardSnapshotDto {
  cinders: number;
  characterExperience: number;
  essenceExperience: number;
  soulstones: number;
  sigilFragments: number;
  propheticFavor: number;
  fateEcho: number;
  cacheItemId?: string | null;
  items: RewardItemSnapshotDto[];
}

export interface RewardItemSnapshotDto {
  itemId: string;
  quantity: number;
}

export interface WeeklyRevelationProgressDto {
  periodStart: string;
  periodEnd: string;
  propheticFavor: number;
  milestones: WeeklyRevelationMilestoneDto[];
}

export interface WeeklyRevelationMilestoneDto {
  favorRequired: number;
  title: string;
  isUnlocked: boolean;
  isClaimed: boolean;
  reward: ProphecyRewardSnapshotDto;
}

export interface ProphecyClaimResponseDto {
  prophecy: ProphecyInstanceDto;
  reward: ProphecyRewardSnapshotDto;
  weeklyRevelation: WeeklyRevelationProgressDto;
}

export interface ClaimWeeklyRevelationMilestoneResponseDto {
  favorRequired: number;
  reward: ProphecyRewardSnapshotDto;
  weeklyRevelation: WeeklyRevelationProgressDto;
}

export interface OpenProphecyCacheResponseDto {
  cacheItemId: string;
  cacheTitle: string;
  reward: ProphecyRewardSnapshotDto;
  rewards: InventoryItem[];
  caches: ProphecyCacheInventoryDto[];
}

@Injectable({
  providedIn: 'root',
})
export class ProphecyService {
  constructor(private readonly api: ApiService) {}

  getOverview(): Observable<PropheciesOverviewDto> {
    return this.api.get('prophecies');
  }

  acceptProphecy(
    id: string,
  ): Observable<VersionedMutationResult<PropheciesOverviewDto>> {
    return this.api.postVersioned<PropheciesOverviewDto>(
      `prophecies/${id}/accept`,
      {},
      {
        stateSyncScopesHandledByResponse: ['prophecies'],
      },
    );
  }

  rerollDailyProphecies(): Observable<
    VersionedMutationResult<PropheciesOverviewDto>
  > {
    return this.api.postVersioned<PropheciesOverviewDto>(
      'prophecies/reroll',
      {},
      {
        stateSyncScopesHandledByResponse: ['prophecies'],
      },
    );
  }

  claimProphecy(
    id: string,
  ): Observable<VersionedMutationResult<ProphecyClaimResponseDto>> {
    return this.api.postVersioned<ProphecyClaimResponseDto>(
      `prophecies/${id}/claim`,
      {},
      {
        stateSyncScopesHandledByResponse: ['prophecies'],
      },
    );
  }

  claimWeeklyMilestone(
    favorRequired: number,
  ): Observable<
    VersionedMutationResult<ClaimWeeklyRevelationMilestoneResponseDto>
  > {
    return this.api.postVersioned<ClaimWeeklyRevelationMilestoneResponseDto>(
      'prophecies/weekly-revelation/claim',
      { favorRequired },
      { stateSyncScopesHandledByResponse: ['prophecies'] },
    );
  }

  openCache(
    cacheItemId: string,
  ): Observable<VersionedMutationResult<OpenProphecyCacheResponseDto>> {
    return this.api.postVersioned<OpenProphecyCacheResponseDto>(
      'prophecies/caches/open',
      { cacheItemId },
      { stateSyncScopesHandledByResponse: ['prophecies'] },
    );
  }
}
