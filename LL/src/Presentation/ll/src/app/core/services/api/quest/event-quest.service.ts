import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { EventQuestJournal } from '../../../../shared/models/event-quest';
import { ApiResponse } from '../../../../shared/models/response';
import { ApiService, VersionedMutationResult } from '../api.service';

const EVENT_QUEST_MUTATION_HANDLED_SCOPES = ['event-quests'] as const;

@Injectable({ providedIn: 'root' })
export class EventQuestService {
  constructor(private readonly api: ApiService) {}

  getJournal(): Observable<EventQuestJournal> {
    return this.api.get('EventQuest');
  }

  claim(eventQuestId: string): Observable<VersionedMutationResult<EventQuestJournal>> {
    return this.api
      .postVersioned<EventQuestJournal | ApiResponse<EventQuestJournal>>(
        `EventQuest/${encodeURIComponent(eventQuestId)}/claim`,
        {},
        {
          stateSyncScopesHandledByResponse:
            EVENT_QUEST_MUTATION_HANDLED_SCOPES,
        },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrap(response.data),
        })),
      );
  }

  claimMilestone(
    eventQuestId: string,
    milestoneKey: string,
  ): Observable<VersionedMutationResult<EventQuestJournal>> {
    return this.api
      .postVersioned<EventQuestJournal | ApiResponse<EventQuestJournal>>(
        `EventQuest/${encodeURIComponent(eventQuestId)}/milestones/${encodeURIComponent(milestoneKey)}/claim`,
        {},
        {
          stateSyncScopesHandledByResponse:
            EVENT_QUEST_MUTATION_HANDLED_SCOPES,
        },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrap(response.data),
        })),
      );
  }

  claimAllMilestones(
    eventQuestId: string,
  ): Observable<VersionedMutationResult<EventQuestJournal>> {
    return this.api
      .postVersioned<EventQuestJournal | ApiResponse<EventQuestJournal>>(
        `EventQuest/${encodeURIComponent(eventQuestId)}/milestones/claim-all`,
        {},
        {
          stateSyncScopesHandledByResponse:
            EVENT_QUEST_MUTATION_HANDLED_SCOPES,
        },
      )
      .pipe(
        map((response) => ({
          ...response,
          data: this.unwrap(response.data),
        })),
      );
  }

  private unwrap(
    response: EventQuestJournal | ApiResponse<EventQuestJournal>,
  ): EventQuestJournal {
    if ('isSuccess' in response) {
      if (!response.isSuccess || !response.data) {
        throw new Error(response.errorMessage ?? 'Request failed');
      }
      return response.data;
    }
    return response;
  }
}
