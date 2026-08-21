import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { StartDungeonRequest } from '../../../../shared/models/requestDtos/dungeons/startDungeonRequest';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { DungeonHubData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonRecordsData } from '../../../../shared/models/Dtos/dungeons/dungeonRecordsData';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { DungeonMasteryBenefitSummary } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';

export enum DungeonRunStatus {
  Active = 'Active',
  Completed = 'Completed',
  Failed = 'Failed',
  Retreated = 'Retreated',
  RewardsClaimed = 'RewardsClaimed',
}

export enum RoomType {
  Unknown = 'Unknown',
  Combat = 'Combat',
  MiniBoss = 'MiniBoss',
  Boss = 'Boss',
  RestSite = 'RestSite',
  Entrance = 'Entrance',
}

export enum RoomInstanceStatus {
  Pending = 'Pending',
  Active = 'Active',
  Completed = 'Completed',
}

export interface RoomInstance {
  id: string;
  index: number;
  type: RoomType;
  status: RoomInstanceStatus;
  encounterIds: string[];
}

export interface DungeonRun {
  id: string;
  characterId: string;
  dungeonDefinitionId: string;
  dungeonDefinitionName: string;
  seed: number;
  status: DungeonRunStatus;
  currentRoomIndex: number;
  rooms: RoomInstance[];
  pendingExperience: number;
  pendingCinders: number;
  pendingSoulstones: number;
  pendingRewards: RunReward[];
  state: DungeonRunState;
  createdAt: string;
  completedAt?: string | null;
}

export interface DungeonRunState {
  masteryLevelAtStart?: number;
  masteryBenefits?: DungeonMasteryBenefitSummary;
  securedLoot: DungeonLootBag;
  pendingLoot: DungeonLootBag;
  mapNodes: DungeonMapNode[];
  traversedRoomIndexes: number[];
  currentRouteOptions: DungeonRouteOption[];
  masteryAwardReasons: DungeonMasteryAwardReason[];
  vigor: number;
  vigorState: string;
  vigorThresholds: DungeonVigorThreshold[];
  currentSection: number;
  totalSections: number;
  restSitesVisited: number;
  lastConsequence: string;
  expiresAt: string;
  vigorHistory: DungeonVigorChange[];
  failureAnalysis?: DungeonFailureAnalysis | null;
}

export interface DungeonVigorThreshold {
  state: string;
  minimumVigor: number;
  maximumVigor: number;
  summary: string;
  effects: string[];
  isCurrent: boolean;
}

export interface DungeonMapNode {
  id: string;
  displayName: string;
  roomIndex: number;
  depth: number;
  lane: number;
  section: number;
  forecast: string;
  vigorCostMin: number;
  vigorCostMax: number;
  nextRoomIndexes: number[];
}

export interface DungeonVigorChange {
  roomIndex: number;
  amount: number;
  vigorAfter: number;
  reason: string;
}

export interface DungeonFailureAnalysis {
  location: string;
  section: number;
  primaryCause: string;
  explanation: string;
  suggestions: string[];
  lostPendingLoot: DungeonLootBag;
}

export interface DungeonLootBag {
  experience: number;
  cinders: number;
  soulstones: number;
  items: Record<string, number>;
}

export interface DungeonMasteryAwardReason {
  id: string;
  description: string;
  experience: number;
}

export interface DungeonRouteOption {
  id: string;
  roomIndex: number;
  displayName: string;
  roomType: RoomType;
  riskLevel: number;
  vigorCostMin: number;
  vigorCostMax: number;
  forecast: string;
}

export interface RunReward {
  itemId: string;
  name: string;
  itemType: string;
  quantity: number;
  source: string;
}

export interface ExecuteDungeonActionRequest {
  actionId: string;
  payload?: unknown;
}

export interface ExecuteDungeonActionResponse {
  run: DungeonRun;
  outcome: DungeonActionOutcome;
  combatSession?: CombatSessionDto | null;
  message?: string | null;
}

export interface ClaimDungeonRewardsResponse {
  activeRun: DungeonRun | null;
  inventoryItems: InventoryItem[];
  claimedLoot: InventoryItem[];
  character: CharacterDto;
}

export interface DismissFailedDungeonRunResponse {
  activeRun: DungeonRun | null;
}

export interface StartDungeonRunResponse {
  run: DungeonRun;
  inventoryItems?: InventoryItem[] | null;
}

export interface DungeonSigilAssemblyResponse {
  dungeonId: string;
  sigilItemId: string;
  sigilName: string;
  inventoryQuantity: number;
  sigilFragmentsRemaining: number;
}

export enum DungeonActionOutcome {
  None = 0,
  CombatVictory = 1,
  CombatDefeat = 2,
  RestSiteResolved = 4,
  RunRetreated = 5,
  RunCompleted = 6,
  RunFailed = 7,
}

@Injectable({
  providedIn: 'root',
})
export class DungeonService {
  constructor(private readonly api: ApiService) {}

  getAvailableDungeons(): Observable<DungeonHubData> {
    return this.api.get('dungeon/getAvailableDungeons').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get available dungeons'));
      }),
    );
  }

  assembleSigil(dungeonId: string): Observable<DungeonSigilAssemblyResponse> {
    return this.api
      .post(`dungeon/${encodeURIComponent(dungeonId)}/assemble-sigil`)
      .pipe(catchError((error) => throwError(() => error)));
  }

  getDungeonRecords(familyId: string): Observable<DungeonRecordsData> {
    return this.api.get(`dungeon/getDungeonRecords/${familyId}`).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get dungeon records'));
      }),
    );
  }

  getActiveDungeon(): Observable<DungeonRun | null> {
    return this.api.get('dungeon/getActiveDungeon').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get active dungeon'));
      }),
    );
  }

  startDungeon(
    request: StartDungeonRequest,
  ): Observable<StartDungeonRunResponse> {
    return this.api.post('dungeon/startDungeon', request).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to start dungeon'));
      }),
    );
  }

  executeDungeonAction(
    runId: string,
    request: ExecuteDungeonActionRequest,
  ): Observable<ExecuteDungeonActionResponse> {
    return this.api.post(`dungeon/executeAction/${runId}`, request).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to progress dungeon'));
      }),
    );
  }

  claimDungeonRewards(): Observable<ClaimDungeonRewardsResponse> {
    return this.api.post('dungeon/claimDungeonRewards').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to claim dungeon rewards'));
      }),
    );
  }

  dismissFailedDungeonRun(): Observable<DismissFailedDungeonRunResponse> {
    return this.api.post('dungeon/dismissFailedDungeonRun').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to leave failed dungeon'));
      }),
    );
  }
}
