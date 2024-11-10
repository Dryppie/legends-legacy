import { Injectable } from '@angular/core';
import {
  BattleOutcome,
  CombatResultDto,
} from '../../../shared/models/Dtos/combatResultDto';
import { BehaviorSubject, delay, of, Subscription } from 'rxjs';
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

    this.allSubscriptions.forEach((subscription) => subscription.unsubscribe());
    this.allSubscriptions = [];
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

  private allSubscriptions: Subscription[] = [];
  constructor() {}

  startCombatSimulation(characterAction: CharacterActionDto): void {
    this.clearCurrentCombat();
    if (!characterAction.combatResult) return;
    this.nextCombatSubject.next(characterAction.updatedAt);

    const combatAction = characterAction.combatResult;
    if (combatAction.eventLog.length < 1) {
      return;
    }
    // Emit the entire combat result

    // Convert StartedAt to milliseconds
    const combatStartTime = new Date(combatAction.startedAt).getTime();
    const now = Date.now();
    const elapsedTime = (now - combatStartTime) / 1000; // in seconds

    // Process each event
    combatAction.eventLog.forEach((event) => {
      const eventTime = event.timestamp / 10; // in seconds since combat started
      const delayTime = (eventTime - elapsedTime) * 1000; // Convert to milliseconds

      const eventSubscription = (
        delayTime <= 0 ? of(event) : of(event).pipe(delay(delayTime))
      ).subscribe((e) => this.combatEventSubject.next(e));

      this.allSubscriptions.push(eventSubscription);
    });

    this.combatResultSubject.next(combatAction);

    // Calculate remaining combat duration
    const combatDurationMs = combatAction.duration * 100; // Corrected to 100ms per unit
    const remainingDuration = combatStartTime + combatDurationMs - now;

    const outcomeSubscription = (
      remainingDuration <= 0
        ? of(combatAction.outcome)
        : of(combatAction.outcome).pipe(delay(remainingDuration))
    ).subscribe((outcome) => {
      this.combatOutcomeSubject.next(outcome);
    });

    this.allSubscriptions.push(outcomeSubscription); // Track outcome subscription
  }
}
