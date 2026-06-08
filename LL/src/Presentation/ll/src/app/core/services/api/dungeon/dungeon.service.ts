import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { StartDungeonRequest } from '../../../../shared/models/requestDtos/dungeons/startDungeonRequest';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';

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
  InProgress = 'InProgress',
  Completed = 'Completed',
  Failed = 'Failed',
  Skipped = 'Skipped',
}

export enum EventOutcomeType {
  Treasure = 'Treasure',
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
  createdAt: string;
  completedAt?: string | null;
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

  // getAvailableDungeons(): Observable<DungeonPreviewData[]> {
  //   return this.api.get('dungeon/getAvailableDungeons').pipe(
  //     catchError(() => {
  //       return throwError(() => new Error('Failed to get available dungeons'));
  //     }),
  //   );
  // }

  getActiveDungeon(): Observable<DungeonRun | null> {
    return this.api.get('dungeon/getActiveDungeon').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get active dungeon'));
      }),
    );
  }

  startDungeon(request: StartDungeonRequest): Observable<DungeonRun> {
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

  claimDungeonRewards(): Observable<void> {
    return this.api.post('dungeon/claimDungeonRewards').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to claim dungeon rewards'));
      }),
    );
  }

  dismissFailedDungeonRun(): Observable<void> {
    return this.api.post('dungeon/dismissFailedDungeonRun').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to leave failed dungeon'));
      }),
    );
  }
}
