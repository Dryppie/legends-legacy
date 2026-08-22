import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { ApiResponse } from '../../../../shared/models/response';
import {
  CombatAreaAccess,
  QuestJournal,
} from '../../../../shared/models/quest';
import { ApiService, VersionedMutationResult } from '../api.service';

const QUEST_MUTATION_HANDLED_SCOPES = ['quests'] as const;
const QUEST_ENCOUNTER_HANDLED_SCOPES = ['inventory'] as const;

@Injectable({ providedIn: 'root' })
export class QuestService {
  constructor(private readonly api: ApiService) {}

  getJournal(): Observable<QuestJournal> {
    return this.api.get('Quest');
  }

  getAreaAccess(): Observable<CombatAreaAccess[]> {
    return this.api.get('Quest/area-access');
  }

  selectChoice(
    questId: string,
    optionKey: string,
  ): Observable<VersionedMutationResult<QuestJournal>> {
    return this.api
      .postVersioned<QuestJournal | ApiResponse<QuestJournal>>(
        `Quest/${encodeURIComponent(questId)}/choice`,
        { optionKey },
        { stateSyncScopesHandledByResponse: QUEST_MUTATION_HANDLED_SCOPES },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrapResponse<QuestJournal>(response.data),
        })),
      );
  }

  acknowledgeWelcome(): Observable<VersionedMutationResult<QuestJournal>> {
    return this.api
      .postVersioned<QuestJournal | ApiResponse<QuestJournal>>(
        'Quest/welcome/acknowledge',
        {},
        { stateSyncScopesHandledByResponse: QUEST_MUTATION_HANDLED_SCOPES },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrapResponse<QuestJournal>(response.data),
        })),
      );
  }

  pin(questId: string | null): Observable<VersionedMutationResult<QuestJournal>> {
    return this.api
      .putVersioned<QuestJournal | ApiResponse<QuestJournal>>(
        'Quest/pinned',
        { questId },
        { stateSyncScopesHandledByResponse: QUEST_MUTATION_HANDLED_SCOPES },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrapResponse<QuestJournal>(response.data),
        })),
      );
  }

  startEncounter(
    questId: string,
    encounterKey: string,
  ): Observable<VersionedMutationResult<CombatResultDto>> {
    return this.api
      .postVersioned<CombatResultDto | ApiResponse<CombatResultDto>>(
        `Quest/${encodeURIComponent(questId)}/encounters/${encodeURIComponent(encounterKey)}/start`,
        {},
        {
          stateSyncScopesHandledByResponse: QUEST_ENCOUNTER_HANDLED_SCOPES,
        },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrapResponse<CombatResultDto>(response.data),
        })),
      );
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
