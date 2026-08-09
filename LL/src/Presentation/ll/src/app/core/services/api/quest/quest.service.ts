import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { ApiResponse } from '../../../../shared/models/response';
import {
  CombatAreaAccess,
  QuestJournal,
} from '../../../../shared/models/quest';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class QuestService {
  constructor(private readonly api: ApiService) {}

  getJournal(): Observable<QuestJournal> {
    return this.api.get('Quest');
  }

  getAreaAccess(): Observable<CombatAreaAccess[]> {
    return this.api.get('Quest/area-access');
  }

  selectChoice(questId: string, optionKey: string): Observable<QuestJournal> {
    return this.api
      .post(`Quest/${encodeURIComponent(questId)}/choice`, { optionKey })
      .pipe(map((response) => this.unwrapResponse<QuestJournal>(response)));
  }

  acknowledgeWelcome(): Observable<QuestJournal> {
    return this.api
      .post('Quest/welcome/acknowledge', {})
      .pipe(map((response) => this.unwrapResponse<QuestJournal>(response)));
  }

  pin(questId: string | null): Observable<QuestJournal> {
    return this.api
      .put('Quest/pinned', { questId })
      .pipe(map((response) => this.unwrapResponse<QuestJournal>(response)));
  }

  startEncounter(
    questId: string,
    encounterKey: string,
  ): Observable<CombatResultDto> {
    return this.api
      .post(
        `Quest/${encodeURIComponent(questId)}/encounters/${encodeURIComponent(encounterKey)}/start`,
        {},
      )
      .pipe(map((response) => this.unwrapResponse<CombatResultDto>(response)));
  }

  private unwrapResponse<T>(response: T | ApiResponse<T>): T {
    if (
      response &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    ) {
      const apiResponse = response as ApiResponse<T>;
      if (!apiResponse.isSuccess || apiResponse.data == null) {
        throw new Error(apiResponse.errorMessage ?? 'Request failed');
      }

      return apiResponse.data;
    }

    return response as T;
  }
}
