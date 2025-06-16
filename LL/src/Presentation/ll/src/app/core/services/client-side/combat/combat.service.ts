import { effect, Injectable } from '@angular/core';
import { delay, of } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CombatStateService } from '../../../state/combat-state/combat-state.service';
import { EventBusService } from '../event-bus/event-bus.service';
import { BattleType } from '../../../state/combat-state/combatState';
import { CombatPlaybackService } from './combat-playback/combat-playback-service';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({
  providedIn: 'root',
})
export class CombatService {
  constructor(
    private playback: CombatPlaybackService,
    private combatStateService: CombatStateService,
    private eventBus: EventBusService,
  ) {
    effect(() => {
      if (this.eventBus.logout()) {
        this.handleLogout();
      }
    });
  }

  clearAllCombat() {
    this.clearCurrentCombat();
  }

  clearCurrentCombat() {
    this.combatStateService.resetCombatState(BattleType.Idle);
  }

  startColosseumMatchSimulation(combatResult: CombatResultDto): void {
    if (!combatResult) return;
    combatResult.battleType = BattleType.Colosseum;
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startCombatSimulation(characterAction: CharacterActionDto): void {
    const combatResult = characterAction.combatSession?.combatResult;
    if (!combatResult) return;
    combatResult.battleType = BattleType.Idle;
    this.clearCurrentCombat();

    this.combatStateService.setNextCombatIn(
      combatResult.battleType,
      characterAction.updatedAt,
    );

    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  simulateFight(combatResult: CombatResultDto) {
    this.combatStateService.setPlayerCharacters(
      combatResult.battleType,
      combatResult.playerTeam,
    );
    this.combatStateService.setEnemyCharacters(
      combatResult.battleType,
      combatResult.enemyTeam,
    );

    const combatAction = combatResult;
    if (combatAction.eventLog.length < 1) {
      return;
    }

    // Convert StartedAt to milliseconds
    const combatStartTime = new Date(combatAction.startedAt).getTime();
    const now = Date.now();

    this.playback.play(combatResult).subscribe({
      next: (ev) =>
        this.combatStateService.addCombatEvent(combatResult.battleType, ev),
      // complete: () => this.onFinished(combatResult.battleType, combatResult),
    });

    this.combatStateService.setCombatResult(
      combatResult.battleType,
      combatAction,
    );

    // Calculate remaining combat duration
    const combatDurationMs = combatAction.duration * 100; // Corrected to 100ms per unit
    const remainingDuration = combatStartTime + combatDurationMs + 1000 - now;

    const onComplete = (combatResult: CombatResultDto) => {
      this.combatStateService.setCombatOutcome(
        combatResult.battleType,
        combatResult.outcome,
      );
      this.handleCombatComplete(combatResult);
    };

    if (remainingDuration <= 0) {
      of(combatAction).subscribe(onComplete);
    } else {
      of(combatAction).pipe(delay(remainingDuration)).subscribe(onComplete);
    }
  }

  private handleCombatComplete(combatResult: CombatResultDto) {
    if (combatResult.battleType === BattleType.Colosseum) {
      this.combatStateService.setCombatActive(combatResult.battleType, false);
      this.combatStateService.resetCombatState(combatResult.battleType);
    }
  }

  /** stop & forget a particular fight (e.g. UI tab closed) */
  stop(battleType: BattleType) {
    this.playback.stop(); // cancels *all* streams; adapt if you need per‑session cancel
    this.combatStateService.resetCombatState(battleType);
  }

  handleLogout() {
    this.clearCurrentCombat();
  }
}
