import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../shared/models/enums/dungeonDifficulty';
import { StartDungeonRequest } from '../../../../shared/models/requestDtos/dungeons/startDungeonRequest';

export interface ActiveDungeonRoom {
  index: number;
  type: string;
  title?: string;
  description?: string;
  completed: boolean;
}

export interface ActiveDungeonRun {
  runId: string;
  dungeonId: string;
  dungeonTitle: string;
  difficulty: DungeonDifficulty;
  currentRoomIndex: number;
  totalRooms: number;
  rooms: ActiveDungeonRoom[];
  canClaimReward: boolean;
  canLeave: boolean;
  isCompleted: boolean;
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

  getActiveDungeon(): Observable<ActiveDungeonRun | null> {
    return this.api.get('dungeon/getActiveDungeon').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get active dungeon'));
      }),
    );
  }

  startDungeon(request: StartDungeonRequest): Observable<ActiveDungeonRun> {
    return this.api.post('dungeon/startDungeon', request).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to start dungeon'));
      }),
    );
  }

  progressDungeon(): Observable<ActiveDungeonRun> {
    return this.api.post('dungeon/progressDungeon').pipe(
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
}
