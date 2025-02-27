import { Injectable } from '@angular/core';
import { delay, of, Subscription } from 'rxjs';
import { CharacterActionDto } from '../../../shared/models/Dtos/characterActionDto';
import { CombatStateService } from '../../state/combat-state/combat-state.service';
import { LevelingService } from '../leveling/leveling.service';
import { EventBusService } from '../event-bus/event-bus.service';

@Injectable({
  providedIn: 'root',
})
export class CombatService {
  private allSubscriptions: Subscription[] = [];

  constructor(
    private combatStateService: CombatStateService,
    private levelingService: LevelingService,
    private eventBusService: EventBusService,
  ) {
    this.eventBusService.logout$.subscribe(() => {
      this.handleLogout();
    });
  }

  clearCurrentCombat() {
    this.allSubscriptions.forEach((subscription) => subscription.unsubscribe());
    this.allSubscriptions = [];
    this.combatStateService.resetCombatState();
  }

  startCombatSimulation(characterAction: CharacterActionDto): void {
    if (!characterAction.combatResult) return;
    this.clearCurrentCombat();

    this.combatStateService.setNextCombatIn(characterAction.updatedAt);

    this.combatStateService.setCombatActive(true);
    this.combatStateService.setPlayerCharacters(
      characterAction.combatResult.playerTeam,
    );
    this.combatStateService.setEnemyCharacters(
      characterAction.combatResult.enemyTeam,
    );

    const combatAction = characterAction.combatResult;
    if (combatAction.eventLog.length < 1) {
      return;
    }

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
      ).subscribe((e) => this.combatStateService.addCombatEvent(e));

      this.allSubscriptions.push(eventSubscription);
    });

    this.combatStateService.setCombatResult(combatAction);

    // Calculate remaining combat duration
    const combatDurationMs = combatAction.duration * 100; // Corrected to 100ms per unit
    const remainingDuration = combatStartTime + combatDurationMs - now;

    const outcomeSubscription = (
      remainingDuration <= 0
        ? of(combatAction.outcome)
        : of(combatAction.outcome).pipe(delay(remainingDuration))
    ).subscribe((outcome) => {
      this.combatStateService.setCombatOutcome(outcome);
      this.levelingService.gainExperience(combatAction.experienceGained);
    });

    this.allSubscriptions.push(outcomeSubscription); // Track outcome subscription
  }

  handleLogout() {
    this.clearCurrentCombat();
  }
}
