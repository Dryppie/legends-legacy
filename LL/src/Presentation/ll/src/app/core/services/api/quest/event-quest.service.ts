import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { EventQuestJournal } from '../../../../shared/models/event-quest';
import { ApiResponse } from '../../../../shared/models/response';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class EventQuestService {
  constructor(private readonly api: ApiService) {}

  getJournal(): Observable<EventQuestJournal> {
    return this.api.get('EventQuest');
  }

  claim(eventQuestId: string): Observable<EventQuestJournal> {
    return this.api
      .post(`EventQuest/${encodeURIComponent(eventQuestId)}/claim`, {})
      .pipe(map((response) => this.unwrap(response)));
  }

  claimMilestone(
    eventQuestId: string,
    milestoneKey: string,
  ): Observable<EventQuestJournal> {
    return this.api
      .post(
        `EventQuest/${encodeURIComponent(eventQuestId)}/milestones/${encodeURIComponent(milestoneKey)}/claim`,
        {},
      )
      .pipe(map((response) => this.unwrap(response)));
  }

  claimAllMilestones(eventQuestId: string): Observable<EventQuestJournal> {
    return this.api
      .post(
        `EventQuest/${encodeURIComponent(eventQuestId)}/milestones/claim-all`,
        {},
      )
      .pipe(map((response) => this.unwrap(response)));
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
