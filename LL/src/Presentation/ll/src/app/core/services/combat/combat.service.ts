import { Injectable } from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { BehaviorSubject, delay, of } from 'rxjs';
import { CombatEvent } from '../../../shared/models/Dtos/combatEventDto';
import { CharacterActionDto } from '../../../shared/models/Dtos/characterActionDto';

@Injectable({
  providedIn: 'root',
})
export class CombatService {
  clearCurrentCombat() {
    this.combatEventSubject.next(null);
    this.combatResultSubject.next(null);
    this.combatOutcomeSubject.next(null);
    this.nextCombatSubject.next(null);
  }

  private combatEventSubject = new BehaviorSubject<CombatEvent | null>(null);
  private combatResultSubject = new BehaviorSubject<CombatResultDto | null>(
    null,
  );
  private combatOutcomeSubject = new BehaviorSubject<BattleOutcome | null>(
    null,
  );
  private nextCombatSubject = new BehaviorSubject<Date | null>(null);

  public combatEvents$ = this.combatEventSubject.asObservable();
  public combatResult$ = this.combatResultSubject.asObservable();
  public combatOutcome$ = this.combatOutcomeSubject.asObservable();
  public nextCombat$ = this.nextCombatSubject.asObservable();
  constructor() {}

  startCombatSimulation(characterAction: CharacterActionDto): void {
    this.nextCombatSubject.next(characterAction.updatedAt);
    if (!characterAction.combatResult) return;
    const combatAction = characterAction.combatResult;
    if (combatAction.eventLog.length === 0) {
      return;
    }
    // Emit the entire combat result
    this.combatResultSubject.next(combatAction);

    // Convert StartedAt to milliseconds
    const combatStartTime = new Date(combatAction.startedAt).getTime();
    const now = Date.now();
    const elapsedTime = (now - combatStartTime) / 1000; // in seconds
    // Process each event
    combatAction.eventLog.forEach((event) => {
      const eventTime = event.timestamp / 10; // in seconds since combat started
      const delayTime = (eventTime - elapsedTime) * 1000; // Convert to milliseconds

      if (delayTime <= 0) {
        // Event has already occurred, emit immediately
        this.combatEventSubject.next(event);
      } else {
        // Schedule the event
        of(event)
          .pipe(delay(delayTime))
          .subscribe((e) => this.combatEventSubject.next(e));
      }
    });

    // Calculate remaining combat duration
    const combatDurationMs = combatAction.duration * 100; // Corrected to 100ms per unit
    const remainingDuration = combatStartTime + combatDurationMs - now;
    if (remainingDuration <= 0) {
      // Combat has ended, emit outcome immediately
      this.combatOutcomeSubject.next(combatAction.outcome);
    } else {
      // Schedule to emit the outcome after the remaining duration
      of(combatAction.outcome)
        .pipe(delay(remainingDuration))
        .subscribe((outcome) => this.combatOutcomeSubject.next(outcome));
    }
  }

  // getCombatEvents(): ObservableOb<CombatEvent> {
  //   return this.combatEventSubject.asObservable();
  // }

  // getCombatResult(): Observable<CombatResultDto> {
  //   return this.combatResult$.asObservable();
  // }

  // getCombatOutcome(): Observable<BattleOutcome> {
  //   return this.combatOutcome$.asObservable();
  // }

  // getNextCombat(): Observable<Date> {
  //   return this.nextCombat$.asObservable();
  // }
}
