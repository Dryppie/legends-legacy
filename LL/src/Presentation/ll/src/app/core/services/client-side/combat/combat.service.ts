import { effect, Injectable } from '@angular/core';
import { bufferTime, delay, filter, of, Subscription } from 'rxjs';
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
  private combatEndSubscriptions = new Map<BattleType, Subscription>();
  private combatEventSubscriptions = new Map<BattleType, Subscription>();

  constructor(
    private playback: CombatPlaybackService,
    private combatStateService: CombatStateService,
    private eventBus: EventBusService,
  ) {
    effect(
      () => {
        if (this.eventBus.logout()) {
          this.handleLogout();
        }
      },
      { allowSignalWrites: true },
    );
  }

  clearAllCombat() {
    this.clearCurrentCombat(BattleType.IdleCombat);
  }

  clearCurrentCombat(type: BattleType) {
    this.combatStateService.resetCombatState(type);
  }

  startDungeonCombatSimulation(combatResult: CombatResultDto | null): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Dungeon;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startColosseumMatchSimulation(combatResult: CombatResultDto): void {
    if (!combatResult) return;

    combatResult.battleType = BattleType.Colosseum;
    this.clearCurrentCombat(combatResult.battleType);
    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  startCombatSimulation(characterAction: CharacterActionDto): void {
    const combatResult = characterAction.combatSession?.combatResult;
    if (!combatResult) return;
    combatResult.battleType = BattleType.IdleCombat;
    this.combatStateService.resetCombatStateForNextBattle(
      combatResult.battleType,
    );

    this.combatStateService.setNextCombatIn(
      combatResult.battleType,
      characterAction.updatedAt,
    );

    this.combatStateService.setCombatActive(combatResult.battleType, true);

    this.simulateFight(combatResult);
  }

  simulateFight(combatResult: CombatResultDto) {
    const type = combatResult.battleType;
    const combatAction = combatResult;
    this.combatStateService.setPlayerCharacters(type, combatResult.playerTeam);
    this.combatStateService.setEnemyCharacters(type, combatResult.enemyTeam);
    this.combatStateService.setCombatResult(type, combatAction);
    this.combatStateService.setEntityStats(type, combatAction.entityStats);

    const combatStartTime = new Date(combatAction.startedAt).getTime();
    const now = Date.now();

    if (combatAction.eventLog.length > 0) {
      this.combatEventSubscriptions.get(type)?.unsubscribe();
      const eventSub = this.playback
        .play(combatResult)
        .pipe(
          bufferTime(16),
          filter((events) => events.length > 0),
        )
        .subscribe({
          next: (events) =>
            this.combatStateService.addCombatEvents(type, events),
          complete: () => this.combatEventSubscriptions.delete(type),
        });
      this.combatEventSubscriptions.set(type, eventSub);
    }

    const combatDurationMs = combatAction.duration * 100;
    const remainingDuration =
      combatStartTime + 10000 /* combatDurationMs + 3000 */ - now;
    const minimumDisplayMs = type === BattleType.IdleCombat ? 0 : 3000;

    const onComplete = (finalResult: CombatResultDto) => {
      // Defensive: skip execution if combat was deactivated
      if (!this.combatStateService.getIsCombatActive(type)()) return;

      this.combatStateService.setCombatOutcome(type, finalResult.outcome);
      this.handleCombatComplete(finalResult);
    };

    const complete$ = of(combatAction).pipe(
      delay(Math.max(minimumDisplayMs, remainingDuration)),
    );

    const sub = complete$.subscribe(onComplete);

    if (type === BattleType.Colosseum || type === BattleType.Dungeon) {
      this.combatEndSubscriptions.get(type)?.unsubscribe();
      this.combatEndSubscriptions.set(type, sub);
    }
  }

  skipCurrentColosseum(): void {
    const type = BattleType.Colosseum;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    // Cancel the delayed completion subscription
    this.combatEndSubscriptions.get(type)?.unsubscribe();
    this.combatEndSubscriptions.delete(type);

    const alreadyPlayed = this.combatStateService.getLastEventsLength(type);
    this.combatStateService.addCombatEvents(
      type,
      combatResult.eventLog.slice(alreadyPlayed),
    );

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);

    this.handleCombatComplete(combatResult);
  }

  skipCurrentDungeonMatch(): void {
    const type = BattleType.Dungeon;

    if (!this.combatStateService.getIsCombatActive(type)()) return;

    const combatResult = this.combatStateService.getCombatResult(type)();
    if (!combatResult) return;

    // Cancel the delayed completion subscription
    this.combatEndSubscriptions.get(type)?.unsubscribe();
    this.combatEndSubscriptions.delete(type);

    const alreadyPlayed = this.combatStateService.getLastEventsLength(type);
    this.combatStateService.addCombatEvents(
      type,
      combatResult.eventLog.slice(alreadyPlayed),
    );

    this.combatStateService.setCombatOutcome(type, combatResult.outcome);
    this.combatStateService.setCombatActive(type, false);

    this.handleCombatComplete(combatResult);
  }

  private handleCombatComplete(combatResult: CombatResultDto) {
    const type = combatResult.battleType;

    if (type === BattleType.Colosseum) {
      this.combatEndSubscriptions.get(type)?.unsubscribe();
      this.combatEndSubscriptions.delete(type);
      this.eventBus.emit('colosseum-combat-finished', {
        outcome: combatResult.outcome, // 'Victory' | 'Defeat' | 'Draw'
      });
    }

    this.combatStateService.setCombatActive(type, false);
    this.stop(type);
  }

  /** stop & forget a particular fight (e.g. UI tab closed) */
  stop(battleType: BattleType) {
    this.combatEventSubscriptions.get(battleType)?.unsubscribe();
    this.combatEventSubscriptions.delete(battleType);
    this.combatEndSubscriptions.get(battleType)?.unsubscribe();
    this.combatEndSubscriptions.delete(battleType);
    this.combatStateService.resetCombatState(battleType);
  }

  handleLogout() {
    this.combatEventSubscriptions.forEach((sub) => sub.unsubscribe());
    this.combatEventSubscriptions.clear();
    this.combatEndSubscriptions.forEach((sub) => sub.unsubscribe());
    this.combatEndSubscriptions.clear();
    this.clearCurrentCombat(BattleType.IdleCombat);
  }
}
