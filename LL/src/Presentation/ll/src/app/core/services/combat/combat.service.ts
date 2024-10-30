import { Injectable } from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { concatMap, delay, from, Observable, of, Subject } from 'rxjs';
import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';

@Injectable({
  providedIn: 'root',
})
export class CombatService {
  private combatEvents$ = new Subject<CombatEvent>();
  private combatResult$ = new Subject<CombatResultDto>();
  private combatOutcome$ = new Subject<BattleOutcome>();
  constructor() {}

  startCombatSimulation(combatResult: CombatResultDto): void {
    if (combatResult.eventLog.length === 0) return;
    // Emit the entire combat result
    this.combatResult$.next(combatResult);

    // Convert StartedAt to milliseconds
    const combatStartTime = new Date(combatResult.startedAt).getTime();
    const now = Date.now();
    const elapsedTime = (now - combatStartTime) / 1000; // in seconds
    // Process each event
    combatResult.eventLog.forEach((event) => {
      const eventTime = event.timestamp / 10; // in seconds since combat started
      const delayTime = (eventTime - elapsedTime) * 1000; // Convert to milliseconds

      if (delayTime <= 0) {
        // Event has already occurred, emit immediately
        this.combatEvents$.next(event);
      } else {
        // Schedule the event
        of(event)
          .pipe(delay(delayTime))
          .subscribe((e) => this.combatEvents$.next(e));
      }
    });

    // Calculate remaining combat duration
    const combatDurationMs = combatResult.duration * 100; // Corrected to 100ms per unit
    const remainingDuration = combatStartTime + combatDurationMs - now;
    if (remainingDuration <= 0) {
      // Combat has ended, emit outcome immediately
      this.combatOutcome$.next(combatResult.outcome);
    } else {
      // Schedule to emit the outcome after the remaining duration
      of(combatResult.outcome)
        .pipe(delay(remainingDuration))
        .subscribe((outcome) => this.combatOutcome$.next(outcome));
    }
  }

  getCombatEvents(): Observable<CombatEvent> {
    return this.combatEvents$.asObservable();
  }

  getCombatResult(): Observable<CombatResultDto> {
    return this.combatResult$.asObservable();
  }

  getCombatOutcome(): Observable<BattleOutcome> {
    return this.combatOutcome$.asObservable();
  }
}
