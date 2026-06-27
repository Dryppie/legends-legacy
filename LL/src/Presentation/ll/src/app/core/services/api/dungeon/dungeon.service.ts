import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { StartDungeonRequest } from '../../../../shared/models/requestDtos/dungeons/startDungeonRequest';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonRecordsData } from '../../../../shared/models/Dtos/dungeons/dungeonRecordsData';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';

export enum DungeonRunStatus {
  Active = 'Active',
  Completed = 'Completed',
  Failed = 'Failed',
  Withdrawn = 'Withdrawn',
  Abandoned = 'Abandoned',
  RewardsClaimed = 'RewardsClaimed',
}

export enum RoomType {
  Unknown = 'Unknown',
  Combat = 'Combat',
  MiniBoss = 'MiniBoss',
  Boss = 'Boss',
  Event = 'Event',
  Treasure = 'Treasure',
  Shrine = 'Shrine',
  Trap = 'Trap',
  Checkpoint = 'Checkpoint',
}

export enum RoomInstanceStatus {
  Pending = 'Pending',
  Active = 'Active',
  Completed = 'Completed',
}

export enum EventOutcomeType {
  ExtraCombat = 'ExtraCombat',
  TreasureRoom = 'TreasureRoom',
  Shrine = 'Shrine',
  Trap = 'Trap',
}

export interface RoomInstance {
  id: string;
  index: number;
  type: RoomType;
  status: RoomInstanceStatus;
  encounterIds: string[];
  eventOutcome?: EventOutcomeType | null;
}

export interface DungeonRun {
  id: string;
  characterId: string;
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
  pressure: number;
  mechanicId: string;
  mechanicDisplayName: string;
  mechanicMaxValue: number;
  rewardMultiplierPercent: number;
  activeBoonIds: string[];
  activeBoonSummaries: DungeonActiveBoonSummary[];
  activeBoonEffectSummaries: DungeonBoonEffectSummary[];
  flags: Record<string, number>;
  securedLoot: DungeonLootBag;
  unsecuredLoot: DungeonLootBag;
  currentRouteOptions: DungeonRouteOption[];
  currentEventChoices: DungeonEventChoiceOption[];
  currentCheckpointChoices: DungeonCheckpointChoiceOption[];
  currentBoonChoices: DungeonBoonChoiceOption[];
  currentBossModifiers: DungeonBossModifier[];
  currentMechanicThresholds: DungeonMechanicThresholdState[];
  masteryAwardReasons: DungeonMasteryAwardReason[];
  combatStyle?: DungeonCombatStyleSnapshot | null;
}

export interface DungeonCombatStyleSnapshot {
  styleId: string;
  styleName: string;
  level: number;
  experience: number;
  selectedFocusId?: string | null;
  selectedFocusName?: string | null;
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
  pressureDelta: number;
  isUnknown: boolean;
  tags: string[];
  possibleRewards: string[];
  requirements: string[];
}

export interface DungeonEventChoiceOption {
  id: string;
  label: string;
  description: string;
  pressureDelta: number;
  rewardMultiplierDeltaPercent: number;
  addFlags: string[];
  removeFlags: string[];
  missingRequirements?: string[];
  grantsBoonChoice: boolean;
  grantsLoot: boolean;
  ambushChancePercent: number;
  revealsHiddenRoute: boolean;
}

export interface DungeonCheckpointChoiceOption {
  id: string;
  label: string;
  description: string;
  pressureDelta: number;
  rewardMultiplierDeltaPercent: number;
}

export interface DungeonBoonChoiceOption {
  id: string;
  familyId: string;
  familyName: string;
  name: string;
  description: string;
  rarity: string;
  tier: number;
  currentStacks: number;
  maxStacks: number;
  currentFamilyStacks: number;
  maxFamilyStacks: number;
  effectSummaries: string[];
}

export interface DungeonActiveBoonSummary {
  id: string;
  familyId: string;
  familyName: string;
  name: string;
  description: string;
  rarity: string;
  tier: number;
  count: number;
  maxFamilyStacks: number;
  effectSummaries: string[];
}

export interface DungeonBoonEffectSummary {
  id: string;
  label: string;
  value: string;
  category: string;
}

export interface DungeonBossModifier {
  id: string;
  name: string;
  description: string;
  source: string;
  attributeType: string;
  amount: number;
  modifierType: string;
  isHelpfulToPlayer: boolean;
}

export interface DungeonMechanicThresholdState {
  id: string;
  value: number;
  description: string;
  rewardMultiplierBonusPercent: number;
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

export enum DungeonActionOutcome {
  None = 0,
  CombatVictory = 1,
  CombatDefeat = 2,
  EventResolved = 3,
  CheckpointResolved = 4,
  RunAbandoned = 5,
  RunCompleted = 6,
}

@Injectable({
  providedIn: 'root',
})
export class DungeonService {
  constructor(private readonly api: ApiService) {}

  getAvailableDungeons(): Observable<DungeonPreviewData[]> {
    return this.api.get('dungeon/getAvailableDungeons').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get available dungeons'));
      }),
    );
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

  startDungeon(request: StartDungeonRequest): Observable<StartDungeonRunResponse> {
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

  leaveDungeon(): Observable<void> {
    return this.api.post('dungeon/leaveDungeon').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to leave dungeon'));
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
