import {
  Subscription,
  catchError,
  expand,
  EMPTY,
  timer,
  mergeMap,
  Observable,
  of,
} from 'rxjs';
import { environment } from '../../../../../../environments/environment';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { Injectable } from '@angular/core';
import { TimeSyncService } from '../../time-sync/time-sync.service';

@Injectable({ providedIn: 'root' })
export class CharacterActionsPollingService {
  private readonly minPollDelayMs = 1_000;
  private readonly maxImmediateBackoffMs = 30_000;
  private readonly recentPollDecisions: Array<{
    actionType: CharacterActionType;
    updatedAt: string;
    rawDelayMs: number;
    scheduledDelayMs: number;
    timestamp: number;
  }> = [];
  private sub: Subscription | null = null;
  private consecutiveImmediatePolls = 0;

  constructor(private timeSync: TimeSyncService) {
    this.installDebugApi();
  }

  start(
    fetch: () => Observable<CharacterActionDto | null>,
    onUpdate: (action: CharacterActionDto | null) => void,
    initialAction?: CharacterActionDto | null,
  ): void {
    this.stop(); // ensure only one poller is active

    const firstAction$ =
      initialAction === undefined ? fetch() : of(initialAction);

    this.sub = firstAction$
      .pipe(
        expand((action) => {
          if (
            !action ||
            action.isDeleted ||
            action.characterActionType === CharacterActionType.Idle
          ) {
            return EMPTY;
          }

          const updatedAt = new Date(action.updatedAt).getTime();
          const now = this.timeSync.now();

          const rawDelay =
            action.characterActionType === CharacterActionType.Combat
              ? updatedAt - now
              : environment.baseDuration * 1000 - (now - updatedAt);
          const nextDelay = this.clampPollDelay(action, rawDelay);

          return timer(nextDelay).pipe(mergeMap(() => fetch()));
        }),
        catchError((err) => {
          console.error('Polling error:', err);
          return EMPTY;
        }),
      )
      .subscribe(onUpdate);
  }

  stop(): void {
    this.sub?.unsubscribe();
    this.sub = null;
    this.consecutiveImmediatePolls = 0;
  }

  private clampPollDelay(
    action: CharacterActionDto,
    rawDelayMs: number,
  ): number {
    let scheduledDelayMs: number;

    if (rawDelayMs <= 0) {
      this.consecutiveImmediatePolls += 1;
      scheduledDelayMs = Math.min(
        this.maxImmediateBackoffMs,
        this.minPollDelayMs * 2 ** (this.consecutiveImmediatePolls - 1),
      );

      if (this.consecutiveImmediatePolls >= 3) {
        console.warn(
          '[CharacterActionsPolling] Prevented immediate polling loop',
          {
            consecutiveImmediatePolls: this.consecutiveImmediatePolls,
            rawDelayMs,
            scheduledDelayMs,
            actionType: action.characterActionType,
            updatedAt: action.updatedAt,
          },
        );
      }
    } else {
      this.consecutiveImmediatePolls = 0;
      scheduledDelayMs = Math.max(rawDelayMs, this.minPollDelayMs);
    }

    this.recordPollDecision(action, rawDelayMs, scheduledDelayMs);
    return scheduledDelayMs;
  }

  private recordPollDecision(
    action: CharacterActionDto,
    rawDelayMs: number,
    scheduledDelayMs: number,
  ): void {
    this.recentPollDecisions.push({
      actionType: action.characterActionType,
      updatedAt: String(action.updatedAt),
      rawDelayMs: Math.round(rawDelayMs),
      scheduledDelayMs: Math.round(scheduledDelayMs),
      timestamp: Date.now(),
    });

    while (this.recentPollDecisions.length > 50) {
      this.recentPollDecisions.shift();
    }
  }

  private installDebugApi(): void {
    if (typeof window === 'undefined') return;

    const debugWindow = window as any;
    debugWindow.__characterActionPollingDebug = {
      recentDecisions: () => [...this.recentPollDecisions],
      printRecentDecisions: () => console.table(this.recentPollDecisions),
      isRunning: () => !!this.sub,
    };
  }
}
